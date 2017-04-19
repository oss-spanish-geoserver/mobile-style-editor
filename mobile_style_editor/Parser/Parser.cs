﻿using System;
using System.Collections.Generic;
using System.IO;
//using System.IO.Compression;
using System.Reflection;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace mobile_style_editor
{
	public class Parser
	{
		//const string BaseStyle = "cartodark";
		const string BaseStyle = "bright-cartocss-style";
		const string UpdatedStyle = "updated-" + BaseStyle;

		const string MSSExtension = ".mss";
		public const string ZipExtension = ".zip";

		public static string ApplicationFolder {
            get
            {
#if __UWP__
                return Windows.Storage.ApplicationData.Current.LocalFolder.Path;
#else
                return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
#endif
            }
        }

		static string FileName { get { return BaseStyle + ZipExtension; } }
		static string FullFilePath { get { return Path.Combine(ApplicationFolder, FileName); } }

		public static ZipData GetZipData(string folder, string filename)
		{
			ZipData data = new ZipData();

			string fullPath = Path.Combine(folder, filename);
			string newFolder = filename.Replace(ZipExtension, "");
			string decompressedPath = Path.Combine(folder, newFolder);

			List<string> paths = Decompress(fullPath, decompressedPath);

			data.Filename = filename;
			data.DecompressedPath = decompressedPath;
			data.FilePaths = paths;

			foreach (string path in paths)
			{
#if __UWP__
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    using (var streamReader = new StreamReader(stream))
                    {
                        string content = streamReader.ReadToEnd();
                        data.DecompressedFiles.Add(content);
                    }
                }
#else
                using (var streamReader = new StreamReader(path))
                {
                    string content = streamReader.ReadToEnd();
                    data.DecompressedFiles.Add(content);
                }
#endif
            }

            return data;
		}

		public static string ZipData(string source, string newFilename)
		{
			FastZip instance = new FastZip();
			instance.CreateEmptyDirectories = true;

			string destination = Path.Combine(ApplicationFolder, newFilename + ZipExtension);

			instance.CreateZip(destination, source, true, "");

			return destination;
		}

		public static List<string> Decompress(string archiveFilenameIn, string outFolder)
		{
			/* 
             * NB Need to manually link encoding for iOS:
			 * Options -> iOS Build -> Additional mtouch aruments: -i18n=west
			 * (http://stackoverflow.com/questions/4600923/monotouch-icsharpcode-sharpziplib-giving-an-error)
			 */

			List<string> paths = new List<string>();

			ZipFile zf = null;
			try
			{
				FileStream fs = File.OpenRead(archiveFilenameIn);

				zf = new ZipFile(fs);

				foreach (ZipEntry zipEntry in zf)
				{
					if (!zipEntry.IsFile)
					{
						// Ignore directories
						continue;
					}

					string entryFileName = zipEntry.Name;

					if (entryFileName.Contains(MSSExtension))
					{
						paths.Add(Path.Combine(outFolder, entryFileName));
						Console.WriteLine("Unzipped .mss file: " + entryFileName);
					}

					// To remove the folder from the entry:- entryFileName = Path.GetFileName(entryFileName);
					// Optionally, match entrynames against a selection list here to skip as desired.
					// The unpacked length is available in the zipEntry.Size property.

					byte[] buffer = new byte[4096];     // 4K is optimum
					Stream zipStream = zf.GetInputStream(zipEntry);

					// Manipulate the output filename here as desired.
					string fullZipToPath = Path.Combine(outFolder, entryFileName);
					string directoryName = Path.GetDirectoryName(fullZipToPath);

					if (directoryName.Length > 0)
					{
						Directory.CreateDirectory(directoryName);
					}

					// Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
					// of the file, but does not waste memory.
					// The "using" will close the stream even if an exception occurs.
					using (FileStream streamWriter = File.Create(fullZipToPath))
					{
						zipStream.Copy(streamWriter, buffer);
					}
				}
			}
			finally
			{
				if (zf != null)
				{
					zf.IsStreamOwner = true; // Makes close also shut the underlying stream
					zf.Close(); // Ensure we release resources
				}
			}

			return paths;
		}

		public static void Compress(string path, ZipOutputStream zipStream, int folderOffset)
		{
			string[] files = Directory.GetFiles(path);

			foreach (string filename in files)
			{
				FileInfo fi = new FileInfo(filename);

				string entryName = filename.Substring(folderOffset); // Makes the name in zip based on the folder
				entryName = ZipEntry.CleanName(entryName); // Removes drive from name and fixes slash direction
				ZipEntry newEntry = new ZipEntry(entryName);
				newEntry.DateTime = fi.LastWriteTime; // Note the zip format stores 2 second granularity

				newEntry.CompressionMethod = CompressionMethod.Deflated;

				// Specifying the AESKeySize triggers AES encryption. Allowable values are 0 (off), 128 or 256.
				// A password on the ZipOutputStream is required if using AES.
				//   newEntry.AESKeySize = 256;

				// To permit the zip to be unpacked by built-in extractor in WinXP and Server2003, WinZip 8, Java, and other older code,
				// you need to do one of the following: Specify UseZip64.Off, or set the Size.
				// If the file may be bigger than 4GB, or you do not need WinXP built-in compatibility, you do not need either,
				// but the zip will be in Zip64 format which not all utilities can understand.
				//   zipStream.UseZip64 = UseZip64.Off;
				newEntry.Size = fi.Length;

				zipStream.PutNextEntry(newEntry);

				// Zip the file in buffered chunks
				// the "using" will close the stream even if an exception occurs
				byte[] buffer = new byte[4096];
				using (FileStream streamReader = File.OpenRead(filename))
				{
					StreamUtils.Copy(streamReader, zipStream, buffer);
				}
				zipStream.CloseEntry();
			}

			string[] folders = Directory.GetDirectories(path);
			foreach (string folder in folders)
			{
				Compress(folder, zipStream, folderOffset);
			}
		}
	}
}
