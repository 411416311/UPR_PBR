using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ShaderGUI
{
    internal class LBLitShader : BaseShaderGUI
    {
        public enum UseMode
        {
            Scene = 0,
            Role = 1
        }

        public enum ZWriteMode
        {
            Off,
            On
        }

        public enum EmissionEdgeMode
        {
            NONE = 0,
            Edge,
            Ghost
        }

        public enum ExtraPass
        {
            NONE = 0,
            ShadowCaster = 1 <<  0,
            ZWriteAlways = 1 <<1,           
            Outline = 1 << 2,
        }

        private static GUIContent settingQualityLevelText = new GUIContent("使用场景","Scene,有完整的能力。Role时将不受间接光影响。");
        private static GUIContent emissionEdgeText = new GUIContent("边缘光");
        private static GUIContent emissionEdgeWidthText = new GUIContent("边缘光宽度");

        private static GUIContent zwriteText = new GUIContent("Depth ZWrite");
        private static GUIContent extraPassText = new GUIContent("额外Pass");
        private static GUIContent editMoreOptionsText = new GUIContent("编辑更多选项");

        private static GUIContent outlineText = new GUIContent("描边");
        private static GUIContent outlineNoFitText = new GUIContent("屏幕适配");
        private static GUIContent outlineLerpNVText = new GUIContent("插值-法线or体型");
        private static GUIContent outlineColorText = new GUIContent("描边线的颜色");
        private static GUIContent outlineWidthText = new GUIContent("描边宽度");
        private static GUIContent outlineZPostionInCameraText = new GUIContent("描边Z轴偏移");
        private static GUIContent outlineStencilIDText = new GUIContent("描边模板ID");

        private LitGUI.LitProperties litProperties;
        private MaterialProperty settingQualityLevelProp;
        private MaterialProperty emissionEdgeProp;
        private MaterialProperty emissionEdgeWidthProp;

        private MaterialProperty editMoreOptionsProp;
        private MaterialProperty zwriteProp;
        private MaterialProperty extraPassMaskProp;

        private MaterialProperty outlineNoFitProp;
        private MaterialProperty outlineLerpNVProp;
        private MaterialProperty outlineColorProp;
        private MaterialProperty outlineWidthProp;
        private MaterialProperty outlineZPostionInCameraProp;
        private MaterialProperty outlineStencilIDProp;




        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            litProperties = new LitGUI.LitProperties(properties);
            settingQualityLevelProp = BaseShaderGUI.FindProperty("_SettingQualityLevel",properties,false);
            emissionEdgeProp = FindProperty("_EmissionEdge", properties,false);
            emissionEdgeWidthProp = FindProperty("_EmissionEdgeWidth",properties,false);

            editMoreOptionsProp = FindProperty("_EditMoreOptions", properties, false);
            zwriteProp = FindProperty("_ZWrite",properties,false);
            extraPassMaskProp = FindProperty("_ExtraPassMask", properties, false);

            outlineNoFitProp = FindProperty("_OutlineNoFit",properties,false);
            outlineLerpNVProp = FindProperty("_OutlineLerpNV", properties, false);
            outlineColorProp = FindProperty("_OutlineColor", properties, false);
            outlineWidthProp = FindProperty("_OutlineWidth", properties, false);
            outlineZPostionInCameraProp = FindProperty("_OutlineZPostionInCamera", properties, false);
            outlineStencilIDProp = FindProperty("_OutlineStencilID", properties, false);
        }

        public override void MaterialChanged(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            CustomSetMaterialKeywords(material);
        }

        public override void DrawSurfaceOptions(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");
            EditorGUIUtility.labelWidth = 0f; 

            EditorGUI.BeginChangeCheck();
            if (settingQualityLevelProp != null)
            {
                EditorGUI.showMixedValue = settingQualityLevelProp.hasMixedValue;
                DoPopup(settingQualityLevelText, settingQualityLevelProp, Enum.GetNames(typeof(UseMode)));
                EditorGUI.showMixedValue = false;
            }
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in materialEditor.targets)
                    MaterialChanged((Material)obj);
            }

            float surfaceValue = surfaceTypeProp.floatValue;
            base.DrawSurfaceOptions(material);
            if (surfaceValue != surfaceTypeProp.floatValue)
            {
                int value = (int)surfaceTypeProp.floatValue;
                if (value == 0)
                    zwriteProp.floatValue = 1;
                else
                    zwriteProp.floatValue = 0;

                SetExtraPass(material,ExtraPass.ShadowCaster,value == 0);
            }

            bool isEditMore = editMoreOptionsProp != null && (int)editMoreOptionsProp.floatValue == 1;
            materialEditor.ShaderProperty(editMoreOptionsProp,editMoreOptionsText);
            EditorGUI.BeginDisabledGroup(!isEditMore);
            DoPopup(zwriteText, zwriteProp, Enum.GetNames(typeof(ZWriteMode)));
            DoEnumFlag<ExtraPass>(extraPassText, extraPassMaskProp, materialEditor);
            EditorGUI.EndDisabledGroup();
        }

        protected override void DrawEmissionProperties(Material material, bool keyword)
        {
            //Copied
            bool flag = true;
            bool flag2 = emissionMapProp.textureValue != null;
            if (!keyword)
            {
                materialEditor.TexturePropertyWithHDRColor(Styles.emissionMap, emissionMapProp, emissionColorProp, showAlpha: false);
            }
            else
            {
                flag = materialEditor.EmissionEnabledProperty();
                EditorGUI.BeginDisabledGroup(!flag);
                materialEditor.TexturePropertyWithHDRColor(Styles.emissionMap, emissionMapProp, emissionColorProp, showAlpha: false);
                EditorGUI.EndDisabledGroup();
            }

            float maxColorComponent = emissionColorProp.colorValue.maxColorComponent;
            if (emissionMapProp.textureValue != null && !flag2 && maxColorComponent <= 0f)
            {
                emissionColorProp.colorValue = Color.white;
            }

            if (flag)
            {
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                if (maxColorComponent <= 0f)
                {
                    material.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                }
            }

            //Insert
            if (emissionEdgeProp != null)
            {
                EditorGUI.BeginDisabledGroup(!flag);

                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = emissionEdgeProp.hasMixedValue;
                BaseShaderGUI.DoPopup(emissionEdgeText,emissionEdgeProp,Enum.GetNames(typeof(EmissionEdgeMode)),materialEditor);
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var obj in materialEditor.targets)
                        MaterialChanged((Material)obj);
                }

                EditorGUI.BeginDisabledGroup(emissionEdgeProp.floatValue == 0);
                EditorGUI.showMixedValue = emissionEdgeWidthProp.hasMixedValue;
                materialEditor.ShaderProperty(emissionEdgeWidthProp, emissionEdgeWidthText);
                EditorGUI.EndDisabledGroup();

                EditorGUI.showMixedValue = false;

                EditorGUI.EndDisabledGroup();
            }
        }

        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            CustomSurfaceInput();

            DrawEmissionProperties(material, true);           

            DrawTileOffset(materialEditor, baseMapProp);

            DrawOutline();
        }

        public void DrawOutline()
        {
            //OutLine
            if (outlineWidthProp == null)
                return;

            EditorGUILayout.Space(8);
            bool outLine = ((int)extraPassMaskProp.floatValue & (int)ExtraPass.Outline) != 0;

            var olValue = EditorGUILayout.Toggle(outlineText, outLine);
            if (olValue != outLine)
            {
                foreach (var obj in materialEditor.targets)
                    SetExtraPass((Material)obj, ExtraPass.Outline, olValue);
            }
            EditorGUI.BeginDisabledGroup(!outLine);

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = outlineColorProp.hasMixedValue;
            materialEditor.ShaderProperty(outlineColorProp, outlineColorText);
            EditorGUI.showMixedValue = outlineWidthProp.hasMixedValue;
            materialEditor.ShaderProperty(outlineWidthProp, outlineWidthText);
            EditorGUI.showMixedValue = outlineNoFitProp.hasMixedValue;
            materialEditor.ShaderProperty(outlineNoFitProp, outlineNoFitText);
            /*
            EditorGUI.showMixedValue = outlineLerpNVProp.hasMixedValue;
            materialEditor.ShaderProperty(outlineLerpNVProp, outlineLerpNVText);
            EditorGUI.showMixedValue = outlineZPostionInCameraProp.hasMixedValue;
            materialEditor.ShaderProperty(outlineZPostionInCameraProp, outlineZPostionInCameraText);
            */
            EditorGUI.showMixedValue = outlineStencilIDProp.hasMixedValue;
            materialEditor.ShaderProperty(outlineStencilIDProp, outlineStencilIDText);

            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in materialEditor.targets)
                    MaterialChanged((Material)obj);
            }

            EditorGUI.showMixedValue = false;

            EditorGUI.EndDisabledGroup();
        }

        public void CustomSurfaceInput()
        {
            bool easyMode = litProperties.metallicGlossMap.textureValue == null;
            EditorGUI.BeginDisabledGroup(!easyMode);
            materialEditor.ShaderProperty(litProperties.metallic, new GUIContent("metallic"));
            materialEditor.ShaderProperty(litProperties.smoothness, new GUIContent("smoothness"));
            EditorGUI.EndDisabledGroup();

            EditorGUI.showMixedValue = litProperties.metallicGlossMap.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            materialEditor.TexturePropertySingleLine(new GUIContent("Metallic Gloss AO"), litProperties.metallicGlossMap);
            if (!EditorGUI.EndChangeCheck())
            {
                foreach (var obj in materialEditor.targets)
                    MaterialChanged((Material)obj);
            }
            EditorGUI.showMixedValue = false;

            DrawNormalArea(materialEditor, litProperties.bumpMapProp, litProperties.bumpScaleProp);
        }

        public override void DrawAdvancedOptions(Material material)
        {
            if (litProperties.reflections != null && litProperties.highlights != null)
            {
                EditorGUI.BeginChangeCheck();
                materialEditor.ShaderProperty(litProperties.highlights, LitGUI.Styles.highlightsText);
                materialEditor.ShaderProperty(litProperties.reflections, LitGUI.Styles.reflectionsText);
                if(EditorGUI.EndChangeCheck())
                {
                    MaterialChanged(material);
                }
            }
            base.DrawAdvancedOptions(material);
        }

        private const int queueOffsetRange = 50;
        public static void CustomSetupMaterialBlendMode(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            bool alphaClip = material.GetFloat("_AlphaClip") == 1;
            if (alphaClip)
            {
                material.EnableKeyword("_ALPHATEST_ON");
            }
            else
            {
                material.DisableKeyword("_ALPHATEST_ON");
            }

            var queueOffset = 0; // queueOffsetRange;
            if (material.HasProperty("_QueueOffset"))
                queueOffset = queueOffsetRange - (int)material.GetFloat("_QueueOffset");

            SurfaceType surfaceType = (SurfaceType)material.GetFloat("_Surface");
            if (surfaceType == SurfaceType.Opaque)
            {
                if (alphaClip)
                {
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                }
                else
                {
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                    material.SetOverrideTag("RenderType", "Opaque");
                }
                material.renderQueue += queueOffset;
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                //material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                //material.SetShaderPassEnabled("ShadowCaster", true);
            }
            else
            {
                BlendMode blendMode = (BlendMode)material.GetFloat("_Blend");
                var queue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                // Specific Transparent Mode Settings
                switch (blendMode)
                {
                    case BlendMode.Alpha:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        break;
                    case BlendMode.Premultiply:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                        break;
                    case BlendMode.Additive:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        break;
                    case BlendMode.Multiply:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.EnableKeyword("_ALPHAMODULATE_ON");
                        break;
                }
                // General Transparent Material Settings
                material.SetOverrideTag("RenderType", "Transparent");
                //material.SetInt("_ZWrite", 0);
                material.renderQueue = queue + queueOffset;                
                //material.SetShaderPassEnabled("ShadowCaster", false);
            }
        }

        //Instead of  LitGUI.SetMaterialKeywords
        public static void CustomSetMaterialKeywords(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            //Copied from BaseShaderGUI.SetMaterialKeywords
            material.shaderKeywords = null;
            CustomSetupMaterialBlendMode(material);

            if (material.HasProperty("_ReceiveShadows"))
            {
                CoreUtils.SetKeyword(material, "_RECEIVE_SHADOWS_OFF", material.GetFloat("_ReceiveShadows") == 0f);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                MaterialEditor.FixupEmissiveFlag(material);
            }

            bool flag = (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0;
            if (material.HasProperty("_EmissionEnabled") && !flag)
            {
                flag = (material.GetFloat("_EmissionEnabled") >= 0.5f);
            }

            CoreUtils.SetKeyword(material, "_EMISSION", flag);
            if (material.HasProperty("_BumpMap"))
            {
                CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap"));
            }

            //Modified from LitShader.SetMaterialKeywords

            CoreUtils.SetKeyword(material, "_METALLICSPECGLOSSMAP", (material.GetTexture("_MetallicGlossMap") != null));

            if (material.HasProperty("_EnvironmentReflections"))
            {
                CoreUtils.SetKeyword(material, "_ENVIRONMENTREFLECTIONS_OFF", material.GetFloat("_EnvironmentReflections") == 0f);
            }

            //Custom Add
            bool isHighQuality = false;
            if (material.HasProperty("_SettingQualityLevel"))
            {
                isHighQuality = material.GetFloat("_SettingQualityLevel") < 1;
                CoreUtils.SetKeyword(material, "_SETTING_QUALITY_LEVEL_1", !isHighQuality);
            }

            if (material.HasProperty("_EmissionEdge"))
            {
                int edgeMode = (int)material.GetFloat("_EmissionEdge");
                CoreUtils.SetKeyword(material, "_EMISSION_EDGE",flag && edgeMode != 0);
                CoreUtils.SetKeyword(material, "_EMISSION_EDGE_GHOST", flag && edgeMode == 2);
            }

            if (material.HasProperty("_ExtraPassMask"))
            {
                int mask = (int)material.GetFloat("_ExtraPassMask");
                bool isZWriteAlways = (mask & (int)ExtraPass.ZWriteAlways) != 0;
                material.SetShaderPassEnabled("ZWriteAlways", isZWriteAlways);

                bool isOutline = (mask & (int)ExtraPass.Outline) != 0;
                material.SetShaderPassEnabled("Outline", isOutline);
                material.SetShaderPassEnabled("Outline_Stencil",isOutline);
                CoreUtils.SetKeyword(material, "TCP2_TANGENT_AS_NORMALS",isOutline && !isHighQuality);
                CoreUtils.SetKeyword(material, "TCP2_COLORS_AS_NORMALS", isOutline && isHighQuality);
                //CoreUtils.SetKeyword(material, "_OUTLINE", isOutline);

                bool isShadowCaster = (mask & (int)ExtraPass.ShadowCaster) != 0;
                material.SetShaderPassEnabled("SHADOWCASTER", isShadowCaster);
            }
        }

        public static void SetExtraPass(Material material,ExtraPass pass,bool isAddOrDel)
        {
            if (!material.HasProperty("_ExtraPassMask"))
                return;

            int mask = (int)material.GetFloat("_ExtraPassMask");
            if (isAddOrDel)
            {
                mask |= (int)pass;               
            }
            else
            {
                mask &=  ~(int)pass;
            }
            material.SetInt("_ExtraPassMask", mask);
        }

        public static void DoEnumFlag<T>(GUIContent label, MaterialProperty property, MaterialEditor materialEditor) where T : System.Enum
        {
            EditorGUI.showMixedValue = property.hasMixedValue;
            float floatValue = property.floatValue;
            EditorGUI.BeginChangeCheck();
            var enumValue = (T)EditorGUILayout.EnumFlagsField(label, System.Enum.ToObject(typeof(T), (int)floatValue) as Enum);
            floatValue = System.Convert.ToInt32(enumValue);
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo(label.text);
                property.floatValue = floatValue;
            }

            EditorGUI.showMixedValue = false;
        }

    }
}
