using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Ninjadini.Neuro.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    public partial class NeuroDataDebugger
    {
        public abstract class ContentProvider : IAssemblyTypeScannable
        {
            public abstract string DropDownName { get; }
            public abstract void CreateGUI(VisualElement container, NeuroDataDebugger window);

            /// If your provider only support a specific format, you can declare it here. return null = everything.
            public virtual Format? GetAllowedFormat() => null;
            
            /// If your provider only support a specific type, you can declare it here. return null = everything.
            public virtual Type GetAllowedType() => null;
            
            public abstract byte[] Load();
            public abstract void Save(byte[] bytes);
            public abstract void Delete();
        }

        class FileContentProvider : ContentProvider
        {
            NeuroDataDebugger _window;
            public override string DropDownName => "File";

            TextField _locationLbl;
            string _filePath;

            public override void CreateGUI(VisualElement container, NeuroDataDebugger window)
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

            public override byte[] Load()
            {
                if (!string.IsNullOrEmpty(_window.srcFilePath))
                {
                    return File.ReadAllBytes(_window.srcFilePath);
                }
                return null;
            }

            public override void Save(byte[] bytes)
            {
                File.WriteAllBytes(_window.srcFilePath, bytes);
            }

            public override void Delete()
            {
                if (!string.IsNullOrEmpty(_window.srcFilePath) 
                    && File.Exists(_window.srcFilePath) 
                    && EditorUtility.DisplayDialog("Delete", _window.srcFilePath, "Delete", "Cancel"))
                {
                    File.Delete(_window.srcFilePath);
                }
            }
        }

        class PersistentDataContentProvider : ContentProvider
        {
            NeuroDataDebugger _window;
            
            public override string DropDownName => "Persistent Data";

            public override void CreateGUI(VisualElement container, NeuroDataDebugger window)
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

            public override byte[] Load()
            {
                if (!string.IsNullOrEmpty(_window.persistentDataName))
                {
                    return File.ReadAllBytes(GetPath());
                }
                return null;
            }

            public override void Save(byte[] bytes)
            {
                if (!string.IsNullOrEmpty(_window.persistentDataName))
                {
                    File.WriteAllBytes(GetPath(), bytes);
                }
            }

            public override void Delete()
            {
                var path = GetPath();
                if (!string.IsNullOrEmpty(_window.persistentDataName) && File.Exists(path))
                {
                    try
                    {
                        if (EditorUtility.DisplayDialog("Delete", path, "Delete", "Cancel"))
                        {
                            File.Delete(path);
                        }
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
            }

            string GetPath() => Application.persistentDataPath + "/" + _window.persistentDataName;
        }

        class TextFieldContentProvider : ContentProvider
        {
            public override string DropDownName => "TextField";
            
            NeuroDataDebugger _window;
            TextField _txtField;

            public override void CreateGUI(VisualElement container, NeuroDataDebugger window)
            {
                _window = window;
                var scrollView = new ScrollView();
                scrollView.style.maxHeight = 300;
                _txtField = new TextField();
                _txtField.multiline = true;
                _txtField.value = _window.srcTxt;
                _txtField.RegisterValueChangedCallback(OnValueChanged);
                
                scrollView.Add(_txtField);
                container.Add(scrollView);
            }

            void OnValueChanged(ChangeEvent<string> evt)
            {
                _window.srcTxt = evt.newValue;
            }

            public override byte[] Load()
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

            public override void Save(byte[] bytes)
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

            public override void Delete()
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