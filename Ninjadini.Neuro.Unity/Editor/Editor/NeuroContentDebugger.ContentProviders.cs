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
    public partial class NeuroContentDebugger
    {
        public abstract class ContentProvider : IAssemblyTypeScannable
        {
            public abstract void CreateGUI(VisualElement container, NeuroContentDebugger window);

            /// If your provider only support a specific format, you can declare it here. return null = everything.
            public virtual Format? GetAllowedFormat() => null;
            
            /// If your provider only support a specific type, you can declare it here. return null = everything.
            public virtual Type GetAllowedType() => null;
            
            public abstract byte[] Load();
            public abstract void Save(byte[] bytes);
            public abstract void Delete();

            protected void ShowLoadNotFoundDialog(string path = null)
            {
                var msg = string.IsNullOrEmpty(path) ? "File not found" : $"File not found @\n{path}";
                EditorUtility.DisplayDialog("Load", msg, "OK");
            }

            protected bool ShowDeleteConfirmDialog(string path)
            {
                return !string.IsNullOrEmpty(path) && EditorUtility.DisplayDialog("Delete", path, "Delete", "Cancel");
            }
        }

        public class FileContentProvider : ContentProvider
        {
            NeuroContentDebugger _window;
            
            TextField _locationLbl;

            public override void CreateGUI(VisualElement container, NeuroContentDebugger window)
            {
                _window = window;

                var fixedPath = GetFixedFilePath();
                var horizontal = NeuroUiUtils.AddHorizontal(container);
                if (string.IsNullOrEmpty(fixedPath))
                {
                    NeuroUiUtils.AddButton(horizontal, "Locate File", OnLocateFileClicked);
                }
                _locationLbl = new TextField();
                _locationLbl.value = string.IsNullOrEmpty(fixedPath) ? _window.srcFilePath : fixedPath;
                _locationLbl.isReadOnly = !string.IsNullOrEmpty(fixedPath);
                _locationLbl.selectAllOnFocus = false;
                _locationLbl.selectAllOnMouseUp = false;
                _locationLbl.style.flexShrink = 1f;
                _locationLbl.style.flexGrow = 1f;
                _locationLbl.RegisterValueChangedCallback(OnLocationTxtChanged);
                horizontal.Add(_locationLbl);
                NeuroUiUtils.AddButton(horizontal, "⊙", OnRevealClicked);
            }

            /// Override me for your custom FileContentProvider with fixed path
            protected virtual string GetFixedFilePath() => null;

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
                var path = GetPath();
                RevealFileOrDirInFinder(path);
            }

            public override byte[] Load()
            {
                var path = GetPath();
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    return File.ReadAllBytes(path);
                }
                ShowLoadNotFoundDialog(path);
                return null;
            }

            public override void Save(byte[] bytes)
            {
                var path = GetPath();
                File.WriteAllBytes(path, bytes);
            }

            public override void Delete()
            {
                var path = GetPath();
                if (!string.IsNullOrEmpty(path) 
                    && File.Exists(path) 
                    && ShowDeleteConfirmDialog(path))
                {
                    File.Delete(path);
                }
            }

            string GetPath() => _locationLbl.value;
        }

        public class PersistentDataContentProvider : ContentProvider
        {
            NeuroContentDebugger _window;
            TextField _nameField;

            public override void CreateGUI(VisualElement container, NeuroContentDebugger window)
            {
                _window = window;
                var fixedName = GetFixedFileName();
                var horizontal = NeuroUiUtils.AddHorizontal(container);
                _nameField = new TextField("FileName");
                _nameField.value = string.IsNullOrEmpty(fixedName) ? window.persistentDataName : fixedName;
                _nameField.isReadOnly = !string.IsNullOrEmpty(fixedName);
                _nameField.selectAllOnFocus = false;
                _nameField.selectAllOnMouseUp = false;
                _nameField.RegisterValueChangedCallback(OnTextFieldChanged);
                horizontal.Add(_nameField);
                NeuroUiUtils.AddButton(horizontal, "⊙", OnRevealClicked);
            }

            /// Override me for your custom PersistentDataContentProvider with fixed path
            protected virtual string GetFixedFileName() => null;

            void OnTextFieldChanged(ChangeEvent<string> evt)
            {
                _window.persistentDataName = evt.newValue;
            }

            void OnRevealClicked()
            {
                RevealFileOrDirInFinder(GetPath(GetFileName()));
            }

            public override byte[] Load()
            {
                var path = GetPath(GetFileName());
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    return File.ReadAllBytes(path);
                }
                ShowLoadNotFoundDialog(path);
                return null;
            }

            public override void Save(byte[] bytes)
            {
                var path = GetPath(GetFileName());
                if (!string.IsNullOrEmpty(path))
                {
                    File.WriteAllBytes(path, bytes);
                }
            }

            public override void Delete()
            {
                var path = GetPath(GetFileName());
                if (!string.IsNullOrEmpty(path) 
                    && File.Exists(path) 
                    && ShowDeleteConfirmDialog(path))
                {
                    File.Delete(path);
                }
            }

            public string GetFileName()
            {
                return _nameField.value;
            }

            public static string GetPath(string fileName)
            {
                return string.IsNullOrEmpty(fileName) ? null : Path.Combine(Application.persistentDataPath, fileName);
            }
        }

        class TextFieldContentProvider : ContentProvider
        {
            NeuroContentDebugger _window;
            TextField _txtField;

            public override void CreateGUI(VisualElement container, NeuroContentDebugger window)
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

        public static void RevealFileOrDirInFinder(string file)
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