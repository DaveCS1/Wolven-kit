﻿using System.Collections.Generic;
using System.IO;
using WolvenKit.CR2W.Editors;
using System.Diagnostics;
using System;

namespace WolvenKit.CR2W.Types
{
    public class CCurve : CVector
    {
            
        public CCurve(CR2WFile cr2w) :
            base(cr2w)
        {

            
        }

        public override void Read(BinaryReader file, uint size)
        {
            base.Read(file, size);

            throw new NotImplementedException();
        }

        public override void Write(BinaryWriter file)
        {
            base.Write(file);

        }

        public override CVariable SetValue(object val)
        {
            return this;
        }

        public override CVariable Create(CR2WFile cr2w)
        {
            return new CCurve(cr2w);
        }

        public override CVariable Copy(CR2WCopyAction context)
        {
            var var = (CCurve) base.Copy(context);


            return var;
        }

        public override List<IEditableVariable> GetEditableVariables()
        {
            return new List<IEditableVariable>(variables)
            {
                
            };
        }
    }
}