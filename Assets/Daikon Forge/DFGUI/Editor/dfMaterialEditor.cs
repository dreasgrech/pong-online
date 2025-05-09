﻿/* Copyright 2013 Daikon Forge */
using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using Object = UnityEngine.Object;

[CanEditMultipleObjects]
[CustomEditor( typeof( Material ), true )]
public class dfMaterialEditor : MaterialEditor
{

#if UNITY_4_3

	protected override void OnHeaderGUI()
	{

		var go = Selection.activeGameObject;
		if( go == null || go.GetComponent<dfGUIManager>() == null )
		{
			base.OnHeaderGUI();
		}

	}

#endif

	public override void OnInspectorGUI()
	{

		var go = Selection.activeGameObject;
		if( go == null || go.GetComponent<dfGUIManager>() == null )
		{
			base.OnInspectorGUI();
		}

	}

}
