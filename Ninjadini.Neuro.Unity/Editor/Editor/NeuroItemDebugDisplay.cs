using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ninjadini.Neuro.Sync;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Neuro.Editor
{
    internal class NeuroItemDebugDisplay : VisualElement
    {
        NeuroReferences refs;
        Func<object> getObjFunc;
        Action<object> setObjFunc;

        Toolbar bottomToolBar;
        ScrollView scrollView;
        TextField debugText;
        VisualElement refsView;
        VisualElement jsonBar;
        Label jsonSizeLbl;

        ToolbarToggle testsToggle;
        ToolbarToggle referncesToggle;
        ToolbarToggle jsonToggle;
        ToolbarToggle binaryToggle;
        ToolbarToggle rawBinaryToggle;

        ReferencedItemsFinder refsFinder;
        ReferencedItemsFinder.SearchReferencesTask refsSearchTask;

        NeuroJsonWriter jsonWriter;
        NeuroBytesWriter _neuroBytesWriter;
        NeuroBytesWriter NeuroBytesWriter => _neuroBytesWriter ??= new NeuroBytesWriter();
        NeuroBytesDebugWalker _bytesWalker;
        NeuroBytesDebugWalker BytesWalker => _bytesWalker ??= new NeuroBytesDebugWalker();

        VisualElement validityStyleTarget;
        
        /// will add the global type id where possible
        public bool PrintAsGlobalType { get; set; }
        
        public NeuroItemDebugDisplay(NeuroReferences references, Func<object> getObj, Action<object> setObj)
        {
            refs = references;
            getObjFunc = getObj;
            setObjFunc = setObj;
            bottomToolBar = new Toolbar();
            bottomToolBar.style.flexShrink = 0f;

            //var liveTestsToggle = AddToolBarToggle(bottomToolBar, "Live", ToggleLiveTests);
            //liveTestsToggle.value = NeuroUnityEditorSettings.LiveContentValidationTestsEnabled;

            testsToggle = AddToolBarToggle(bottomToolBar, "Tests", ToggleTests);
            
            referncesToggle = AddToolBarToggle(bottomToolBar, "References", ToggleReferences);
            jsonToggle = AddToolBarToggle(bottomToolBar, "JSON", ToggleJson);
            binaryToggle = AddToolBarToggle(bottomToolBar, "Binary", ToggleBinary);
            rawBinaryToggle = AddToolBarToggle(bottomToolBar, "Raw Binary", ToggleRawBinary);
            
            testsToggle.style.width = 80;
            //liveTestsToggle.style.width = 50;
            referncesToggle.style.width = 80;
            jsonToggle.style.width = 80;
            binaryToggle.style.width = 80;
            rawBinaryToggle.style.width = 80;
            
            Add(bottomToolBar);
        }
/*
        void ToggleLiveTests(ChangeEvent<bool> evt)
        {
            NeuroUnityEditorSettings.LiveContentValidationTestsEnabled = evt.newValue;
        }
*/

        public void SetReferencesTabEnabled(bool enabled)
        {
            referncesToggle.style.display = enabled ? DisplayStyle.Flex : DisplayStyle.None;
        }
        public void SetTestsTabEnabled(bool enabled)
        {
            testsToggle.style.display = enabled ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        ToolbarToggle AddToolBarToggle(VisualElement parent, string label, EventCallback<ChangeEvent<bool>> callback)
        {
            var jsonToggle = new ToolbarToggle();
            jsonToggle.label = label;
            jsonToggle.RegisterValueChangedCallback(callback);
            parent.Add(jsonToggle);
            return jsonToggle;
        }

        void ToggleTests(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                SetToggleBtn(testsToggle);
                NeuroUiUtils.SetDisplay(jsonBar, false);
            }
            else
            {
                HideDebugPanel();
            }
        }

        void ToggleReferences(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                SetToggleBtn(referncesToggle);
                NeuroUiUtils.SetDisplay(jsonBar, false);
            }
            else
            {
                HideDebugPanel();
            }
        }

        void ToggleJson(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                SetToggleBtn(jsonToggle);
                NeuroUiUtils.SetDisplay(jsonBar, true);
            }
            else
            {
                HideDebugPanel();
            }
        }

        void ToggleBinary(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                SetToggleBtn(binaryToggle);
                NeuroUiUtils.SetDisplay(jsonBar, false);
            }
            else
            {
                HideDebugPanel();
            }
        }

        void ToggleRawBinary(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                SetToggleBtn(rawBinaryToggle);
                NeuroUiUtils.SetDisplay(jsonBar, false);
            }
            else
            {
                HideDebugPanel();
            }
        }

        void SetToggleBtn(Toggle toggle)
        {
            if(jsonToggle != toggle) jsonToggle.SetValueWithoutNotify(false);
            if(binaryToggle != toggle) binaryToggle.SetValueWithoutNotify(false);
            if(rawBinaryToggle != toggle) rawBinaryToggle.SetValueWithoutNotify(false);
            if(referncesToggle != toggle) referncesToggle.SetValueWithoutNotify(false);
            if(testsToggle != toggle) testsToggle.SetValueWithoutNotify(false);
            ShowDebugPanel();
            Refresh();
        }
        
        void ShowDebugPanel()
        {
            if (scrollView == null)
            {
                scrollView = new ScrollView();
                scrollView.style.minHeight = 30;
                scrollView.style.maxHeight = 500;
                scrollView.style.bottom = 0;
                Add(scrollView);
                scrollView.PlaceBehind(bottomToolBar);

                debugText = new TextField();
                debugText.multiline = true;
                debugText.style.whiteSpace = WhiteSpace.Normal;
                debugText.style.flexGrow = 1f;
                scrollView.Add(debugText);

                refsView = new VisualElement();
                scrollView.Add(refsView);

                jsonBar = NeuroUiUtils.AddHorizontal(this);
                jsonBar.style.flexShrink = 0f;
                jsonBar.PlaceBehind(bottomToolBar);
                if (setObjFunc != null)
                {
                    NeuroUiUtils.AddButton(jsonBar, "Apply", OnApplyJson);
                    jsonSizeLbl = NeuroUiUtils.AddLabel(jsonBar, "");
                    jsonSizeLbl.style.unityTextAlign = TextAnchor.MiddleRight;
                    jsonSizeLbl.style.flexGrow = 1f;
                }
            }
            else
            {
                NeuroUiUtils.SetDisplay(scrollView, true);
            }
        }

        void HideDebugPanel()
        {
            NeuroUiUtils.SetDisplay(jsonBar, false);
            NeuroUiUtils.SetDisplay(scrollView, false);
        }

        public void Refresh()
        {
            refsSearchTask?.Cancel();
            refsSearchTask = null;
            
            var obj = getObjFunc();
            if (obj != null)
            {
                UpdateLiveTest(obj);
                if (binaryToggle.value)
                {
                    UpdateBinary(obj);
                }
                else if (rawBinaryToggle.value)
                {
                    UpdateRawBinary(obj);
                }
                else if (jsonToggle.value)
                {
                    UpdateJSON(obj);
                }
                else if (referncesToggle.value)
                {
                    UpdateReferences(obj);
                }
                else if (testsToggle.value)
                {
                    UpdateTestResults(obj);
                }
            }
            else
            {
                debugText.value = "null";
            }
        }

        void UpdateJSON(object obj)
        {
            if (jsonWriter == null)
            {
                jsonWriter = new NeuroJsonWriter(refs);
            }
            var json = ShouldUseGlobalType(obj) ? jsonWriter.WriteGlobalTyped(obj) : jsonWriter.Write(obj);
            debugText.value = json;
            if (jsonSizeLbl != null)
            {
                jsonSizeLbl.text = json.Length + "chars";
            }
            debugText.isReadOnly = false;
            NeuroUiUtils.SetDisplay(debugText, true);
            NeuroUiUtils.SetDisplay(refsView, false);
        }

        bool ShouldUseGlobalType(object obj)
        {
            return obj != null && PrintAsGlobalType && NeuroGlobalTypes.GetIdByType(obj.GetType()) != 0;
        }

        void UpdateBinary(object obj)
        {
            var globalTyped = ShouldUseGlobalType(obj);
            var bytes = (globalTyped ? NeuroBytesWriter.WriteGlobalTyped(obj) : NeuroBytesWriter.Write(obj)).ToArray();
            debugText.value = BytesWalker.Walk(bytes);
            debugText.isReadOnly = true;
            NeuroUiUtils.SetDisplay(debugText, true);
            NeuroUiUtils.SetDisplay(refsView, false);
        }
        
        void UpdateRawBinary(object obj)
        {
            var bytes = (ShouldUseGlobalType(obj) ? NeuroBytesWriter.WriteGlobalTyped(obj) : NeuroBytesWriter.Write(obj)).ToArray();
            debugText.value = RawProtoReader.GetDebugString(bytes);
            debugText.isReadOnly = true;
            NeuroUiUtils.SetDisplay(debugText, true);
            NeuroUiUtils.SetDisplay(refsView, false);
        }

        void UpdateReferences(object obj)
        {
            NeuroUiUtils.SetDisplay(debugText, false);
            NeuroUiUtils.SetDisplay(refsView, true);
            if (refsFinder == null)
            {
                refsFinder = new ReferencedItemsFinder();
            }
            refsView.Clear();
            
            if (obj is IReferencable referencable)
            {
                NeuroUiUtils.AddLabel(refsView, "Searching for references...");
                refsSearchTask = ReferencedItemsFinder.SearchReferencesTask.Start(referencable, refs);
                var localTask = refsSearchTask;
                refsView.schedule.Execute(OnRefsViewUpdate).Until(() => localTask != refsSearchTask);
            }
        }

        void OnRefsViewUpdate()
        {
            if (refsSearchTask is { Busy: false })
            {
                refsView.Clear();
                refsSearchTask.DrawResultToUI(refsView, GotoRef);
                refsSearchTask = null;
            }
        }

        NeuroContentTester tester;
        List<string> testResults;
        StringBuilder testResultsBuilder = new StringBuilder();
        void UpdateTestResults(object obj)
        {
            NeuroUiUtils.SetDisplay(debugText, true);
            NeuroUiUtils.SetDisplay(refsView, false);
            debugText.isReadOnly = true;
            testResultsBuilder.Clear();
            testResultsBuilder.Append("Test took ").Append(tester.TimeTaken.TotalMilliseconds).Append("ms\n");
            if (testResults.Count > 0)
            {
                testResultsBuilder.AppendJoin("\n", testResults);
            }
            else if(tester.ValidatorsVisited.Count == 0)
            {
                testResultsBuilder.Append("No validator matches found.\nMake a new class extending from ")
                    .Append(nameof(INeuroContentValidator)).Append("<").Append(GetTypeName(obj.GetType())).Append("> to get started.");
            }
            else
            {
                testResultsBuilder.Append(" \u2714");
                testResultsBuilder.AppendJoin("\n \u2714", tester.ValidatorsVisited.Select(GetTypeName));
            }
            debugText.value = testResultsBuilder.ToString();
        }

        static string GetTypeName(Type type)
        {
            return NeuroEditorUtils.GetTypeName(type);
        }

        void UpdateLiveTest(object obj)
        {
            testResults ??= new List<string>();
            testResults.Clear();
            if (tester == null)
            {
                var context = new NeuroContentValidatorContext(refs, s => testResults.Add(s))
                {
                    TesterSource = parent,
                    TesterName = "NeuroEditor",
                    SkipHeavyTests = true,
                };
                tester = new NeuroContentTester(context);
            }
            if (NeuroGlobalTypes.GetIdByType(obj.GetType()) != 0)
            {
                tester.Test(obj);
            }
            if (testResults.Count > 0)
            {
                testsToggle.label = $"Tests <color=red>[{testResults.Count} fail]</color>";
            }
            else if (tester.ValidatorsVisited.Count > 0)
            {
                testsToggle.label = "Tests [\u2714]";
            }
            else
            {
                testsToggle.label = "Tests [NA]";
            }
            if (validityStyleTarget != null)
            {
                if (testResults.Count > 0)
                {
                    var c = new Color(1f, 0f, 0f, 0.1f);
                    validityStyleTarget.style.backgroundColor = c;
                    testsToggle.style.backgroundColor = c;
                }
                else
                {
                    validityStyleTarget.style.backgroundColor = default;
                    testsToggle.style.backgroundColor = default;
                }
            }
        }

        public void SetValidityStylingTarget(VisualElement target)
        {
            validityStyleTarget = target;
        }

        void GotoRef(IReferencable referencable)
        {
            var loopParent = parent;
            while (loopParent != null)
            {
                if (loopParent is NeuroEditorNavElement navElement)
                {
                    var refType = NeuroReferences.GetRootReferencable(referencable.GetType());
                    navElement.SetSelectedItem(refType, referencable.RefId);
                    break;
                }
                loopParent = loopParent.parent;
            }
        }

        void OnApplyJson()
        {
            object obj = null;
            try
            {
                var existingItem = (IReferencable)getObjFunc();
                var refId = existingItem.RefId;
                var refName = existingItem.RefName;
                obj = existingItem;
                new NeuroJsonReader().Read(debugText.value, ref obj);
                if (obj is IReferencable newItem)
                {
                    newItem.RefId = refId;
                    newItem.RefName = refName;
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error parsing JSON", e.Message, "OK");
                return;
            }

            if (obj == null)
            {
                EditorUtility.DisplayDialog("", "Parsed result is null", "OK");
                return;
            }
            setObjFunc(obj);
        }
    }
}