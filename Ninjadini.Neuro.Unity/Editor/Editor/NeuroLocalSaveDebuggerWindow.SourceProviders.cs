using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Ninjadini.Neuro.Utils;
using UnityEditor;
using UnityEngine.Device;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    public partial class NeuroLocalSaveDebuggerWindow
    {
        interface IContentSourceProvider : IAssemblyTypeScannable
        {
            string DropDownName { get; }
            void CreateGUI(VisualElement container, NeuroLocalSaveDebuggerWindow window);

            byte[] Load();
            void Save(byte[] bytes);
            void Delete();
        }

        class FileSourceProvider : IContentSourceProvider
        {
            NeuroLocalSaveDebuggerWindow _window;
            public string DropDownName => "File";

            TextField _locationLbl;
            string _filePath;

            public void CreateGUI(VisualElement container, NeuroLocalSaveDebuggerWindow window)
            {
                _window = window;
                var horizontal = NeuroUiUtils.AddHorizontal(container);
                NeuroUiUtils.AddButton(horizontal, "Locate File", OnLocateFileClicked);
                _locationLbl = new TextField();
                _locationLbl.value = _window.srcFilePath;
                _locationLbl.style.flexShrink = 1f;
                _locationLbl.RegisterValueChangedCallback(OnLocationTxtChanged);
                horizontal.Add(_locationLbl);
                NeuroUiUtils.AddButton(horizontal, "⊙", OnRevealClicked);
            }

            void OnLocationTxtChanged(ChangeEvent<string> evt)
            {
                _window.srcFilePath = evt.newValue;
            }

            void OnLocateFileClicked()
            {
                var path = EditorUtility.OpenFilePanel("Neuro File", "", "*");
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
                _locationLbl.value = path;
            }

            void OnRevealClicked()
            {
                RevealFileOrDirInFinder(_window.srcFilePath);
            }

            public byte[] Load()
            {
                if (!string.IsNullOrEmpty(_window.srcFilePath))
                {
                    return File.ReadAllBytes(_window.srcFilePath);
                }
                return null;
            }

            public void Save(byte[] bytes)
            {
                File.WriteAllBytes(_window.srcFilePath, bytes);
            }

            public void Delete()
            {
                File.Delete(_window.srcFilePath);
            }
        }

        class PersistentDataSourceProvider : IContentSourceProvider
        {
            NeuroLocalSaveDebuggerWindow _window;
            
            public string DropDownName => "Persistent Data";

            public void CreateGUI(VisualElement container, NeuroLocalSaveDebuggerWindow window)
            {
                _window = window;
                var horizontal = NeuroUiUtils.AddHorizontal(container);
                var textField = new TextField("FileName");
                textField.value = window.persistentDataName;
                textField.RegisterValueChangedCallback(OnTextFieldChanged);
                horizontal.Add(textField);
                NeuroUiUtils.AddButton(horizontal, "⊙", OnRevealClicked);
            }

            void OnTextFieldChanged(ChangeEvent<string> evt)
            {
                _window.persistentDataName = evt.newValue;
            }

            void OnRevealClicked()
            {
                RevealFileOrDirInFinder(GetPath());
            }

            public byte[] Load()
            {
                if (!string.IsNullOrEmpty(_window.persistentDataName))
                {
                    return File.ReadAllBytes(GetPath());
                }
                return null;
            }

            public void Save(byte[] bytes)
            {
                if (!string.IsNullOrEmpty(_window.persistentDataName))
                {
                    File.WriteAllBytes(GetPath(), bytes);
                }
            }

            public void Delete()
            {
                if (!string.IsNullOrEmpty(_window.persistentDataName))
                {
                    try
                    {
                        File.Delete(GetPath());
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
            }

            string GetPath() => Application.persistentDataPath + "/" + _window.persistentDataName;
        }

        class TextFieldSourceProvider : IContentSourceProvider
        {
            public string DropDownName => "TextField";
            
            NeuroLocalSaveDebuggerWindow _window;
            TextField _txtField;

            public void CreateGUI(VisualElement container, NeuroLocalSaveDebuggerWindow window)
            {
                _window = window;
                _txtField = new TextField();
                _txtField.multiline = true;
                _txtField.value = _window.srcTxt;
                _txtField.RegisterValueChangedCallback(OnValueChanged);
                
                container.Add(_txtField);
            }

            void OnValueChanged(ChangeEvent<string> evt)
            {
                _window.srcTxt = evt.newValue;
            }

            public byte[] Load()
            {
                var format = _window.GetSelectedFormat();
                var str = _txtField.value;
                if (format == Format.JSON)
                {
                    return RawProtoWriter.UTF8Encoding.GetBytes(str);
                }
                if(format == Format.Binary)
                {
                    str = Regex.Replace(str, "[^0-9a-zA-Z]", "");
                    var length = str.Length;
                    var numBytes = length / 2;
                    var result = new byte[numBytes];
                    for (var i = 0; i < numBytes; i++)
                    {
                        var byteStr = str.Substring(i * 2, 2);
                        result[i] = Convert.ToByte(byteStr, 16);
                    }
                    return result;
                }
                throw new NotSupportedException("Format not supported, " + format);
            }

            public void Save(byte[] bytes)
            {
                var format = _window.GetSelectedFormat();
                if (format == Format.JSON)
                {
                    _txtField.value = Encoding.UTF8.GetString(bytes);
                }
                else if(format == Format.Binary)
                {
                    _txtField.value = BitConverter.ToString(bytes);
                }
            }

            public void Delete()
            {
                _txtField.value = "";
            }
        }

        static void RevealFileOrDirInFinder(string file)
        {
            if (File.Exists(file))
            {
                EditorUtility.RevealInFinder(file);
            }
            else
            {
                var dir = Path.GetDirectoryName(file);
                var attempts = 0;
                while (!string.IsNullOrEmpty(dir) && attempts < 10)
                {
                    attempts++;
                    if (Directory.Exists(dir))
                    {
                        EditorUtility.OpenWithDefaultApp(dir);
                        break;
                    }
                    else
                    {
                        dir = Path.GetDirectoryName(dir);
                    }
                }
            }
        }
    }
}