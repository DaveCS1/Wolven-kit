﻿using System;
using System.IO;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using WeifenLuo.WinFormsUI.Docking;
using WolvenKit.Common.Services;

namespace WolvenKit
{
    using Bundles;
    using Cache;
    using CR2W;
    using CR2W.Editors;
    using CR2W.Types;
    using W3Strings;
    using Common;
    using App;

    public enum EColorThemes
    {
        VS2015Light = 0,
        VS2015Dark = 1,
        VS2015Blue = 2,
    }

    public class UIController : IVariableEditor, INotifyPropertyChanged
    {
        private static UIController mainController;
        public UIConfiguration Configuration { get; private set; }
        public frmMain Window { get; private set; }

        public const string ManagerCacheDir = "ManagerCache";
        public string VLCLibDir = "C:\\Program Files\\VideoLAN\\VLC";
        public string InitialModProject = "";
        public string InitialWKP = "";

        // Color Themes
        public VisualStudioToolStripExtender ToolStripExtender { get; set; }
        private readonly List<ThemeBase> _themesList = new List<ThemeBase>() { new VS2015LightTheme(), new VS2015DarkTheme(), new VS2015BlueTheme() };
        public ThemeBase GetTheme() => _themesList[(int)Configuration.ColorTheme];
       
        public event PropertyChangedEventHandler PropertyChanged;


        private KeyValuePair<string,Logtype> _logMessage = new KeyValuePair<string, Logtype>("",Logtype.Normal);
        public KeyValuePair<string, Logtype> LogMessage
        {
            get => _logMessage;
            set => SetField(ref _logMessage, value, nameof(LogMessage));
        }

        private UIController()  {  }

        /// <summary>
        /// Useful function for blindly importing a file.
        /// </summary>
        /// <param name="name">The name of the file.</param>
        /// <param name="archive">The manager to search for the file in.</param>
        /// <returns></returns>
        public static List<byte[]> ImportFile(string name,IWitcherArchive archive)
        {
            List<byte[]> ret = new List<byte[]>();
            archive.FileList.ToList().Where(x => x.Name.Contains(name)).ToList().ForEach(x =>
            {
                using (var ms = new MemoryStream())
                {
                    x.Extract(ms);
                    ret.Add(ms.ToArray());
                }
            });
            return ret;
        }

        /// <summary>
        /// Here we setup stuff we need in every form. Borders etc can be done here in the future.
        /// </summary>
        /// <param name="form">The form to initialize.</param>
        public static void InitForm(Form form)
        {
            Bitmap bmp = WolvenKit.Properties.Resources.Logo_wkit;
            form.Icon = Icon.FromHandle(bmp.GetHicon());
        }

        /// <summary>
        /// Queues a string for logging in the main window.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        /// <param name="type">The type of the log. Not needed.</param>
        public void QueueLog(string msg, Logtype type = Logtype.Normal)
        {
            LogMessage = new KeyValuePair<string, Logtype>(msg, type);
        }


        public void CreateVariableEditor(CVariable editvar, EVariableEditorAction action)
        {
            switch (action)
            {
                case EVariableEditorAction.Export:
                    ExportBytes(editvar);
                    break;
                case EVariableEditorAction.Import:
                    ImportBytes(editvar);
                    break;
                case EVariableEditorAction.Open:
                    OpenEditorFor(editvar);
                    break;
            }
        }

        public static UIController Get()
        {
            if (mainController == null)
            {
                mainController = new UIController();
                mainController.Configuration = UIConfiguration.Load();
                mainController.Window = new frmMain();
            }
            return mainController;
        }

        public frmCR2WDocument LoadDocument(string filename, bool suppressErrors = false)
        {
            return Window.LoadDocument(filename, null, suppressErrors);
        }

        public frmCR2WDocument LoadDocument(string filename, MemoryStream memoryStream, bool suppressErrors = false)
        {
            return Window.LoadDocument(filename, memoryStream, suppressErrors);
        }


        private void ImportBytes(CVariable editvar)
        {
            var dlg = new OpenFileDialog() { InitialDirectory = MainController.Get().Configuration.InitialExportDirectory };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                MainController.Get().Configuration.InitialExportDirectory = Path.GetDirectoryName(dlg.FileName);

                using (var fs = new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = new BinaryReader(fs))
                    {
                        var bytes = ImportExportUtility.GetImportBytes(reader);
                        editvar.SetValue(bytes);
                    }
                }
            }
        }

        private void ExportBytes(CVariable editvar)
        {
            var dlg = new SaveFileDialog();
            byte[] bytes = null;

            if (editvar is IByteSource)
            {
                bytes = ((IByteSource) editvar).Bytes;
            }

            dlg.Filter = string.Join("|", ImportExportUtility.GetPossibleExtensions(bytes, editvar.Name));
            dlg.InitialDirectory = MainController.Get().Configuration.InitialExportDirectory;

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                MainController.Get().Configuration.InitialExportDirectory = Path.GetDirectoryName(dlg.FileName);

                using (var fs = new FileStream(dlg.FileName, FileMode.Create, FileAccess.Write))
                {
                    using (var writer = new BinaryWriter(fs))
                    {
                        bytes = ImportExportUtility.GetExportBytes(bytes, Path.GetExtension(dlg.FileName));
                        writer.Write(bytes);
                    }
                }
            }
        }

        private void OpenHexEditorFor(CVariable editvar)
        {
            var editor = new frmHexEditorView() { File = editvar.cr2w };

            if (editvar is IByteSource)
            {
                editor.Bytes = ((IByteSource) editvar).Bytes;
            }

            editor.Text = "Hex Viewer [" + editvar.FullName + "]";
            editor.Show();
        }

        private void OpenEditorFor(CVariable editvar)
        {
            byte[] bytes = null;

            if (editvar is IByteSource)
            {
                bytes = ((IByteSource) editvar).Bytes;
            }

            if (bytes != null)
            {
                var doc = LoadDocument(editvar.cr2w.FileName + ":" + editvar.FullName, new MemoryStream(bytes), true);
                if (doc != null)
                {
                    doc.OnFileSaved += OnVariableEditorSave;
                    doc.SaveTarget = editvar;
                }
                else
                {
                    OpenHexEditorFor(editvar);
                }
            }
        }

        private void OnVariableEditorSave(object sender, FileSavedEventArgs args)
        {
            if (args.Stream is MemoryStream)
            {
                var doc = (frmCR2WDocument) sender;
                var editvar = (CVariable) doc.SaveTarget;
                editvar.SetValue(((MemoryStream) args.Stream).ToArray());
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}