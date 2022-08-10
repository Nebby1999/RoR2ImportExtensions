﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThunderKit.Core.Config;
using ThunderKit.Core.Data;
using ThunderKit.Markdown;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace RiskOfThunder.RoR2Importer
{
    using static ThunderKit.Common.Constants;
    public class MMHookGeneratorConfiguration : OptionalExecutor
    {
        public override string Name => "MMHook Generator";
        public override string Description => "The listed assemblies will have MMHook assemblies generated using HookGenPatcher";
        public override int Priority => Constants.Priority.MMHookGeneratorConfiguration;

        public List<string> assemblyNames = new List<string> { "RoR2.dll", "KinematicCharacterController.dll" };

        public UnityEngine.Object hookGenExecutable;

        private SerializedObject serializedObject;
        private VisualElement rootVisualElement;
        private MarkdownElement MessageElement
        {
            get
            {
                if (_messageElement == null)
                {
                    _messageElement = new MarkdownElement();
                    _messageElement.MarkdownDataType = MarkdownDataType.Text;
                }
                return _messageElement;
            }
        }
        private MarkdownElement _messageElement;

        public override bool Execute() => true;

        protected override VisualElement CreateProperties()
        {
            serializedObject = new SerializedObject(this);
            var executableProperty = serializedObject.FindProperty(nameof(hookGenExecutable));
            var assemblyList = serializedObject.FindProperty(nameof(assemblyNames));
            rootVisualElement = new VisualElement();

            //hookgen should ideally be located automatically, This method should find it if the generator is in Packages, which it should be.
            if (executableProperty.objectReferenceValue == null)
            {
                //If hookgen couldnt be located, display warning
                if (TryToFindHookGenExecutable(out var executable))
                {
                    executableProperty.objectReferenceValue = executable;
                    serializedObject.ApplyModifiedProperties();
                }
                else
                {
                    MessageElement.Data = $"***__WARNING__***: Could not find HookGen Executable! Hover over the \"Hook Gen Executable\" field for instructions.";
                    rootVisualElement.Add(MessageElement);
                }
            }

            PropertyField nstripField = new PropertyField(executableProperty);
            nstripField.tooltip = $"The HookGen executable, this is used for process of creating assemblies for hooking purposes" +
                $"\nIf this field appears to be empty, then the RoR2Importer has failed to find the executable automatically." +
                $"\nPlease select the HookGen executable in your project, if no Executable exists, download the following hookGen:" +
                $"\n https://github.com/Windows10CE/MonoMod/tree/selfcontained";
            nstripField.RegisterCallback<ChangeEvent<UnityEngine.Object>>(OnHookGenSet);
            rootVisualElement.Add(nstripField);

            PropertyField listField = new PropertyField(assemblyList);
            listField.tooltip = $"A list of assembly names to generate Hooking assemblies, Case Sensitive.";
            rootVisualElement.Add(listField);
            return rootVisualElement;
        }

        private void OnHookGenSet(ChangeEvent<UnityEngine.Object> evt)
        {
            var nstrip = evt.newValue;
            if (nstrip == null)
            {
                MessageElement.Data = $"***__WARNING__***: Could not find HookGen Executable! Hover over the \"Hook Gen Executable\" field for instructions.";
                if (!rootVisualElement.Contains(MessageElement))
                {
                    rootVisualElement.Add(MessageElement);
                }
                return;
            }

            var relativePath = AssetDatabase.GetAssetPath(nstrip);
            var fullPath = Path.GetFullPath(relativePath);
            var fileName = Path.GetFileName(fullPath);

            if (fileName != "MonoMod.RuntimeDetour.HookGen.exe")
            {
                MessageElement.Data = $"Object in \"Hoook Gen Executable\" is not HookGen!";
                if (!rootVisualElement.Contains(MessageElement))
                {
                    rootVisualElement.Add(MessageElement);
                }
                return;
            }

            MessageElement.RemoveFromHierarchy();
        }

        private bool TryToFindHookGenExecutable(out UnityEngine.Object nstripExecutable)
        {
            nstripExecutable = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Constants.Paths.HookGenExePath);
            if (!nstripExecutable)
            {
                var nstripPath = AssetDatabase.FindAssets("", FindAllFolders)
                             .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                             .FirstOrDefault(path => path.EndsWith("MonoMod.RuntimeDetour.HookGen.exe"));

                nstripExecutable = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(nstripPath);
            }

            return nstripExecutable;
        }


        internal static MMHookGeneratorConfiguration GetDataStorer()
        {
            var settings = ThunderKitSetting.GetOrCreateSettings<ImportConfiguration>();
            var hookDataStorage = settings.ConfigurationExecutors.OfType<MMHookGeneratorConfiguration>().FirstOrDefault();
            return hookDataStorage;
        }

        public override void Cleanup()
        {
            var publicizedAssemblies = Directory.EnumerateFiles(Constants.Paths.PublicizedAssembliesFolder, "*.dll", SearchOption.AllDirectories);
            foreach (string assemblyPath in publicizedAssemblies)
            {
                File.Delete(assemblyPath);
            }
        }
    }
}