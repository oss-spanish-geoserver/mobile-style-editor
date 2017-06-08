﻿
using System;
using System.Collections.Generic;
using System.Linq;
using Carto.Core;

namespace mobile_style_editor
{
	public class ZipData
	{
		public string Filename { get; set; }

		public string DecompressedPath { get; set; }

		public List<string> AllFilePaths { get; set; }

		public List<string> DecompressedFiles { get; set; }

		public List<string> StyleFilePaths { get; set; }

		public List<string> StyleFileNames { get; set; }

        public List<string> ChangeList { get; set; }

        public int ProjectFileIndex
        {
            get
            { 
                string file = StyleFileNames.Find(f => f.Equals(Parser.ProjectFile));
                return StyleFileNames.IndexOf(file);
            }
        }

        public bool ContainsProjectFile { get { return !ProjectFileIndex.Equals(-1); } }

		public string ProjectFile { get { return DecompressedFiles[ProjectFileIndex]; } }

        public bool ContainsNutiParameters
        {
            get { return !Variant.FromString(ProjectFile).GetObjectElement("nutiparameters").String.Equals("null"); }
        }

		public List<NutiParameter> NutiParameters
		{
			get
			{
				List<NutiParameter> parameters = new List<NutiParameter>();

				Variant json = Variant.FromString(ProjectFile).GetObjectElement("nutiparameters");

                List<NutiEnum> enums = Enum.GetValues(typeof(NutiEnum)).Cast<NutiEnum>().ToList();

                foreach (NutiEnum item in enums)
                {
                    Variant child = json.GetObjectElement(item.ToString());

                    if (child.String != "null")
                    {
						var parameter = new NutiParameter();
						parameter.Type = item;

                        if (parameter.IsBoolean)
                        {
                            List<string> keys = new List<string> { "0", "1" };
                            Variant values = child.GetObjectElement("values");

                            Variant defaultValue = child.GetObjectElement("default");

                            if (item == NutiEnum.buildings3d)
                            {
                                // Only buildingd3d has values as false/true,
                                // texts3d, markers3d, shields3d have point/nutibillboard,
                                // which is still technically a boolean
                                parameter.DefaultValue = defaultValue.Bool;
                            }
                            else
                            {
                                parameter.DefaultValue = defaultValue.String;
                            }

                            foreach (string key in keys)
                            {
                                string value = values.GetObjectElement(key).String;
                                parameter.Values.Add(key, value);
                            }
                        }

                        parameters.Add(parameter);
                    }
                }

				return parameters;
			}
		}

        public ZipData()
		{
			DecompressedFiles = new List<string>();

			StyleFilePaths = new List<string>();

			StyleFileNames = new List<string>();

            ChangeList = new List<string>();
		}

	}
}
