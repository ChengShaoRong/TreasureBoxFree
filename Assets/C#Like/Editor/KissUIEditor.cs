/*
 *           C#Like
 * Copyright © 2022-2024 RongRong. All right reserved.
 * Using TextMeshPro is optional.
 * 
 * If you don't want to use it, you should Add 'DISABLE_TMP' to define symbols,
 * and remove the prefab that name end with '(TMP)' in folder 'Assets/C#Like/Editor/KissUI/',
 * and the demo project 'TreasureBox' using the TextMeshPro, you should remove it too.
 */
using UnityEngine;
using UnityEditor;

namespace CSharpLike
{
	public class KissUIEditor
	{
#if !DISABLE_TMP
		[MenuItem("GameObject/KissUI/KissText(TextMeshPro)")]
		static void AddComponentKissTextTMP(MenuCommand menuCommand)
		{
			LoadPrefab(menuCommand, "KissText(TMP)");
		}
#endif
		[MenuItem("GameObject/KissUI/KissToggle")]
		static void AddComponentKissToggle(MenuCommand menuCommand)
        {
			LoadPrefab(menuCommand, "KissToggle");
		}
#if !DISABLE_TMP
		[MenuItem("GameObject/KissUI/KissToggle(TextMeshPro)")]
		static void AddComponentKissToggleTMP(MenuCommand menuCommand)
		{
			LoadPrefab(menuCommand, "KissToggle(TMP)");
		}
#endif
		[MenuItem("GameObject/KissUI/KissSlider")]
		static void AddComponentKissSlider(MenuCommand menuCommand)
		{
			LoadPrefab(menuCommand, "KissSlider");
		}
		[MenuItem("GameObject/KissUI/KissImage")]
		static void AddComponentKissImage(MenuCommand menuCommand)
		{
			LoadPrefab(menuCommand, "KissImage");
		}
		[MenuItem("GameObject/KissUI/KissButton")]
		static void AddComponentKissButton(MenuCommand menuCommand)
		{
			LoadPrefab(menuCommand, "KissButton");
		}
#if !DISABLE_TMP
		[MenuItem("GameObject/KissUI/KissButton(TextMeshPro)")]
		static void AddComponentKissButtonTMP(MenuCommand menuCommand)
		{
			LoadPrefab(menuCommand, "KissButton(TMP)");
		}
#endif
		[MenuItem("GameObject/KissUI/KissScrollbar")]
		static void AddComponentKissScrollbar(MenuCommand menuCommand)
		{
			LoadPrefab(menuCommand, "KissScrollbar");
		}
		[MenuItem("GameObject/KissUI/KissInputField")]
		static void AddComponentKissInputField(MenuCommand menuCommand)
		{
			LoadPrefab(menuCommand, "KissInputField");
		}
#if !DISABLE_TMP
		[MenuItem("GameObject/KissUI/KissInputField(TextMeshPro)")]
		static void AddComponentKissInputFieldTMP(MenuCommand menuCommand)
		{
			LoadPrefab(menuCommand, "KissInputField(TMP)");
		}
#endif
		[MenuItem("GameObject/KissUI/KissPanel")]
		static void AddComponentKissPanel(MenuCommand menuCommand)
		{
			LoadPrefab(menuCommand, "KissPanel");
		}
#if !DISABLE_TMP
		[MenuItem("GameObject/KissUI/KissPanel(TextMeshPro)")]
		static void AddComponentKissPanelTMP(MenuCommand menuCommand)
		{
			LoadPrefab(menuCommand, "KissPanel(TMP)");
		}
#endif
		[MenuItem("GameObject/KissUI/KissScrollView")]
		static void AddComponentKissScrollView(MenuCommand menuCommand)
		{
			LoadPrefab(menuCommand, "KissScrollView");
		}
		[MenuItem("GameObject/KissUI/KissDropdown")]
		static void AddComponentKissDropdown(MenuCommand menuCommand)
		{
			LoadPrefab(menuCommand, "KissDropdown");
		}
#if !DISABLE_TMP
		[MenuItem("GameObject/KissUI/KissDropdown(TextMeshPro)")]
		static void AddComponentKissDropdownTMP(MenuCommand menuCommand)
		{
			LoadPrefab(menuCommand, "KissDropdown(TMP)");
		}
#endif
		static bool LoadPrefab(MenuCommand menuCommand, string prefabName)
		{
			GameObject goParent = menuCommand.context as GameObject;
			if (goParent == null)
			{
				Debug.LogError("You should select a parent GameObject first!");
				return false;
			}
			string fileName = $"Assets/C#Like/Editor/KissUI/{prefabName}.prefab";
			GameObject go = AssetDatabase.LoadAssetAtPath(fileName, typeof(GameObject)) as GameObject;
			if (go == null)
			{
				Debug.LogError($"Can't load prefab '{fileName}'");
				return false;
			}
			go = GameObject.Instantiate(go);
			if (go == null)
			{
				Debug.LogError($"Can't instantiate prefab '{fileName}'");
				return false;
			}
			go.name = prefabName;
			GameObjectUtility.SetParentAndAlign(go, goParent);
			return true;
		}
	}
}
