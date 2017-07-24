﻿using System;
using System.IO;
using System.Xml;
using OpenBveApi.Math;

namespace OpenBve
{
	internal static class Ls3DGrpParser
	{

		internal class GruppenObject
		{
			//A gruppenobject holds a list of ls3dobjs, which appear to be roughly equivilant to meshbuilders
			internal string Name;
			internal Vector3 Position;
			internal Vector3 Rotation;
			internal string FunctionScript;

			internal GruppenObject()
			{
				Name = string.Empty;
				Position = new Vector3();
				Rotation = new Vector3();
			}
		}

		internal static ObjectManager.AnimatedObjectCollection ReadObject(string FileName, System.Text.Encoding Encoding,
			ObjectManager.ObjectLoadMode LoadMode)
		{
			XmlDocument currentXML = new XmlDocument();
			//May need to be changed to use de-DE
			System.Globalization.CultureInfo Culture = System.Globalization.CultureInfo.InvariantCulture;
			ObjectManager.AnimatedObjectCollection Result = new ObjectManager.AnimatedObjectCollection();
			Result.Objects = new ObjectManager.AnimatedObject[0];
			try
			{
				currentXML.Load(FileName);
			}
			catch (Exception ex)
			{
				//The XML is not strictly valid
				string[] Lines = File.ReadAllLines(FileName);
				using (var stringReader = new StringReader(Lines[0]))
				{
					var settings = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment };
					using (var xmlReader = XmlReader.Create(stringReader, settings))
					{
						if (xmlReader.Read())
						{
							//Attempt to find the text encoding and re-read the file
							var result = xmlReader.GetAttribute("encoding");
							var e = System.Text.Encoding.GetEncoding(result);
							Lines = File.ReadAllLines(FileName, e);
							//Turf out the old encoding, as our string array should now be UTF-8
							Lines[0] = "<?xml version=\"1.0\"?>";
						}
					}
				}
				for (int i = 0; i < Lines.Length; i++)
				{
					while (Lines[i].IndexOf("\"\"") != -1)
					{
						//Loksim parser tolerates multiple quotes, strict XML does not
						Lines[i] = Lines[i].Replace("\"\"", "\"");
					}
					while (Lines[i].IndexOf("  ") != -1)
					{
						//Replace double-spaces with singles
						Lines[i] = Lines[i].Replace("  ", " ");
					}
				}
				bool tryLoad = false;
				try
				{
					//Horrible hack: Write out our string array to a new memory stream, then load from this stream
					//Why can't XmlDocument.Load() just take a string array......
					using (var stream = new MemoryStream())
					{
						var sw = new StreamWriter(stream);
						foreach (var line in Lines)
						{
							sw.Write(line);
							sw.Flush();
						}
						sw.Flush();
						stream.Position = 0;
						currentXML.Load(stream);
						tryLoad = true;
					}
				}
				catch
				{
					//Generic catch-all clause
				}
				if (!tryLoad)
				{
					//Pass out the *original* XML error, not anything generated when we've tried to correct it
					Interface.AddMessage(Interface.MessageType.Error, false, "Error parsing Loksim3D XML: " + ex.Message);
					return null;
				}
			}

			string BaseDir = System.IO.Path.GetDirectoryName(FileName);

			GruppenObject[] CurrentObjects = new GruppenObject[0];
			//Check for null
			if (currentXML.DocumentElement != null)
			{
				ObjectManager.UnifiedObject[] obj = new OpenBve.ObjectManager.UnifiedObject[0];
				XmlNodeList DocumentNodes = currentXML.DocumentElement.SelectNodes("/GRUPPENOBJECT");
				if (DocumentNodes != null)
				{
					foreach (XmlNode outerNode in DocumentNodes)
					{
						if (outerNode.HasChildNodes)
						{
							foreach (XmlNode node in outerNode.ChildNodes)
							{
								if (node.Name == "Object" && node.HasChildNodes)
								{

									foreach (XmlNode childNode in node.ChildNodes)
									{
										if (childNode.Name == "Props" && childNode.Attributes != null)
										{
											GruppenObject Object = new GruppenObject();
											foreach (XmlAttribute attribute in childNode.Attributes)
											{
												switch (attribute.Name)
												{
													case "Name":
														string ObjectFile = OpenBveApi.Path.CombineFile(BaseDir, attribute.Value);
														if (!System.IO.File.Exists(ObjectFile))
														{
															if (attribute.Value.StartsWith("\\Objekte"))
															{
																//This is a reference to the base Loksim3D object directory
																DirectoryInfo d = new DirectoryInfo(BaseDir);
																while (d.Parent != null)
																{
																	//Recurse upwards and try to see if we're in the Loksim directory
																	d = d.Parent;
																	if (d.ToString().ToLowerInvariant() == "objekte")
																	{
																		d = d.Parent;
																		ObjectFile = OpenBveApi.Path.CombineFile(d.FullName, attribute.Value);
																		break;
																	}
																}
															}
															if (!System.IO.File.Exists(ObjectFile))
															{
																//Last-ditch attempt: Check User & Public for the Loksim object directory
																if (Program.CurrentlyRunningOnWindows)
																{
																	ObjectFile = OpenBveApi.Path.CombineFile(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\Loksim3D", attribute.Value);
																	if (!System.IO.File.Exists(ObjectFile))
																	{
																		ObjectFile = OpenBveApi.Path.CombineFile(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments) + "\\Loksim3D", attribute.Value);
																	}
																}
															}
															if (!System.IO.File.Exists(ObjectFile))
															{
																Object.Name = null;
																Interface.AddMessage(Interface.MessageType.Warning, true, "Ls3d Object file " + attribute.Value + " not found.");
																break;
															}
														}
														Object.Name = ObjectFile;
														break;
													case "Position":
														string[] SplitPosition = attribute.Value.Split(';');
														double.TryParse(SplitPosition[0], out Object.Position.X);
														double.TryParse(SplitPosition[1], out Object.Position.Y);
														double.TryParse(SplitPosition[2], out Object.Position.Z);
														break;
													case "Rotation":
														string[] SplitRotation = attribute.Value.Split(';');

														double.TryParse(SplitRotation[0], out Object.Rotation.X);
														double.TryParse(SplitRotation[1], out Object.Rotation.Y);
														double.TryParse(SplitRotation[2], out Object.Rotation.Z);
														break;
													case "ShowOn":
														//Defines when the object should be shown
														Object.FunctionScript = FunctionScripts.GetPostfixNotationFromInfixNotation(GetAnimatedFunction(attribute.Value, false));
														break;
													case "HideOn":
														//Defines when the object should be hidden
														Object.FunctionScript = FunctionScripts.GetPostfixNotationFromInfixNotation(GetAnimatedFunction(attribute.Value, true));
														break;
												}
											}
											if (Object.Name != null)
											{
												Array.Resize<GruppenObject>(ref CurrentObjects, CurrentObjects.Length + 1);
												CurrentObjects[CurrentObjects.Length - 1] = Object;
											}
										}
									}
								}
							}
						}
					}
					//We've loaded the XML references, now load the objects into memory
					for (int i = 0; i < CurrentObjects.Length; i++)
					{
						if (CurrentObjects[i] == null || string.IsNullOrEmpty(CurrentObjects[i].Name))
						{
							continue;
						}
						var Object = Ls3DObjectParser.ReadObject(CurrentObjects[i].Name, LoadMode, CurrentObjects[i].Rotation);
						if (Object != null)
						{
							Array.Resize<ObjectManager.UnifiedObject>(ref obj, obj.Length + 1);
							obj[obj.Length - 1] = Object;

							Array.Resize<ObjectManager.AnimatedObject>(ref Result.Objects, Result.Objects.Length + 1);
							Object.Dynamic = true;
							ObjectManager.AnimatedObject a = new ObjectManager.AnimatedObject();
							ObjectManager.AnimatedObjectState aos = new ObjectManager.AnimatedObjectState
							{
								Object = Object,
								Position = CurrentObjects[i].Position,
							};
							a.States = new ObjectManager.AnimatedObjectState[] { aos };
							Result.Objects[i] = a;
							if (!string.IsNullOrEmpty(CurrentObjects[i].FunctionScript))
							{
								Result.Objects[i].StateFunction =
									FunctionScripts.GetFunctionScriptFromPostfixNotation(CurrentObjects[i].FunctionScript + " 1 == --");
							}
						}
					}
					
				}
				return Result;
			}
			//Didn't find an acceptable XML object
			//Probably will cause things to throw an absolute wobbly somewhere....
			return null;
		}

		private static string GetAnimatedFunction(string Value, bool Hidden)
		{
			string[] splitStrings = Value.Split(' ');
			string script = string.Empty;
			for (int i = 0; i < splitStrings.Length; i++)
			{
				splitStrings[i] = splitStrings[i].Trim().ToLowerInvariant();
				if (i % 2 == 0)
				{
					if (splitStrings[i].StartsWith("spitzenlicht1-an"))
					{
						//Appears to be HEADLIGHTS (F)
						script += Hidden ? "reversernotch != -1" : "reversernotch == -1";
					}
					if (splitStrings[i].StartsWith("schlusslicht1-an"))
					{
						//Appears to be TAILLIGHTS (F)
						script += Hidden ? "reversernotch != 1" : "reversernotch == 1";
					}
					if (splitStrings[i].StartsWith("spitzenlicht2-an"))
					{
						//Appears to be HEADLIGHTS (R)
						script += Hidden ? "reversernotch != 1" : "reversernotch == 1";
					}
					if (splitStrings[i].StartsWith("schlusslicht2-an"))
					{
						//Appears to be TAILLIGHTS (R)
						script += Hidden ? "reversernotch != -1" : "reversernotch == -1";
					}
					if (splitStrings[i].StartsWith("tür") && splitStrings[i].EndsWith("offen"))
					{
						switch (splitStrings[i][3])
						{
							case '0':
							case '2':
							case '4':
							case '6':
							case '8':
								//Left doors (??)
								script += Hidden ? "rightdoors == 0" : "rightdoors != 0";
								break;
							case '1':
							case '3':
							case '5':
							case '7':
							case '9':
								//Right doors (??)
								script += Hidden ? "leftdoors == 0" : "leftdoors != 0";
								break;
						}
					}
					if (splitStrings[i].StartsWith("rauch"))
					{
						//Smoke (e.g. steam loco)
						string[] finalStrings = splitStrings[i].Split('_');
						switch (finalStrings[1])
						{
							case "stand":
								//Standing
								script += Hidden ? "reversernotch != 0 | powernotch != 0" : "reversernotch == 0 | powernotch == 0";
								break;
							case "fahrt":
								switch (finalStrings[2])
								{
									case "vor":
										//Forwards
										script += Hidden ? "reversernotch != 1 & powernotch == 0" : "reversernotch == 1 & powernotch > 0";
										break;
									case "rueck":
										//Reverse
										script += Hidden ? "reversernotch != -1 & powernotch == 0" : "reversernotch == -1 & powernotch > 0";
										break;
								}
								break;
						}
					}
				}
				else
				{
					switch (splitStrings[i].ToLowerInvariant())
					{
						case "or":
							script += " & ";
							break;
						case "and":
							script += " | ";
							break;
					}
				}
			}
			return script;
		}
	}
}