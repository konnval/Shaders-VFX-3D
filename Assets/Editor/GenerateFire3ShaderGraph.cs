using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.ShaderGraph.Internal;

public static class GenerateFire3ShaderGraph
{
    private const string GraphPath = "Assets/Projects/Playrix_VFX_Prog/fire_3.shadergraph";
    private const string MaterialPath = "Assets/Projects/Playrix_VFX_Prog/fire_3.mat";
    private const string SessionKey = "generate_fire_3_shader_graph_once";

    [InitializeOnLoadMethod]
    private static void AutoRunOnce()
    {
        if (SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        SessionState.SetBool(SessionKey, true);
        EditorApplication.delayCall += () =>
        {
            try
            {
                Generate();
            }
            catch (Exception ex)
            {
                Debug.LogError($"fire_3 auto-generation failed: {ex}");
            }
        };
    }

    [MenuItem("Tools/Generate Fire 3 Shader Graph")]
    public static void GenerateMenu()
    {
        Generate();
    }

    public static void Generate()
    {
        var shaderGraphAsm = AppDomain.CurrentDomain.GetAssemblies()
            .First(a => a.GetName().Name == "Unity.ShaderGraph.Editor");
        var urpAsm = AppDomain.CurrentDomain.GetAssemblies()
            .First(a => a.GetName().Name == "Unity.RenderPipelines.Universal.Editor");

        var graphDataType = shaderGraphAsm.GetType("UnityEditor.ShaderGraph.GraphData", true);
        var abstractMaterialNodeType = shaderGraphAsm.GetType("UnityEditor.ShaderGraph.AbstractMaterialNode", true);
        var propertyNodeType = shaderGraphAsm.GetType("UnityEditor.ShaderGraph.PropertyNode", true);
        var blockNodeType = shaderGraphAsm.GetType("UnityEditor.ShaderGraph.BlockNode", true);
        var targetType = shaderGraphAsm.GetType("UnityEditor.ShaderGraph.Target", true);
        var graphUtilType = shaderGraphAsm.GetType("UnityEditor.ShaderGraph.FileUtilities", true);
        var categoryDataType = shaderGraphAsm.GetType("UnityEditor.ShaderGraph.CategoryData", true);
        var universalTargetType = urpAsm.GetType("UnityEditor.Rendering.Universal.ShaderGraph.UniversalTarget", true);
        var spriteUnlitSubTargetType = urpAsm.GetType("UnityEditor.Rendering.Universal.ShaderGraph.UniversalSpriteUnlitSubTarget", true);
        var blockFieldsType = shaderGraphAsm.GetType("UnityEditor.ShaderGraph.BlockFields", true);
        var blockFieldDescriptorType = shaderGraphAsm.GetType("UnityEditor.ShaderGraph.BlockFieldDescriptor", true);
        var precisionType = shaderGraphAsm.GetType("UnityEditor.ShaderGraph.Precision", true);

        var graph = Activator.CreateInstance(graphDataType);
        SetProperty(graph, "assetGuid", AssetDatabase.AssetPathToGUID(GraphPath));
        SetProperty(graph, "path", "Shader Graphs");
        Invoke(graph, "AddContexts");

        var target = Activator.CreateInstance(universalTargetType);
        universalTargetType.GetMethod("TrySetActiveSubTarget", BindingFlags.Instance | BindingFlags.Public)!
            .Invoke(target, new object[] { spriteUnlitSubTargetType });

        var blocks = Array.CreateInstance(blockFieldDescriptorType, 5);
        blocks.SetValue(GetStaticPropertyValue(blockFieldsType, "VertexDescription.Position"), 0);
        blocks.SetValue(GetStaticPropertyValue(blockFieldsType, "VertexDescription.Normal"), 1);
        blocks.SetValue(GetStaticPropertyValue(blockFieldsType, "VertexDescription.Tangent"), 2);
        blocks.SetValue(GetStaticPropertyValue(blockFieldsType, "SurfaceDescription.BaseColor"), 3);
        blocks.SetValue(GetStaticPropertyValue(blockFieldsType, "SurfaceDescription.Alpha"), 4);

        var targets = Array.CreateInstance(targetType, 1);
        targets.SetValue(target, 0);
        Invoke(graph, "InitializeOutputs", targets, blocks);
        Invoke(graph, "AddCategory", InvokeStatic(categoryDataType, "DefaultCategory"));

        var nodes = new Dictionary<string, object>();

        var textMaskProp = AddTextureProperty(shaderGraphAsm, graph, "TextMask", "_TextMask");
        var flameTextureProp = AddTextureProperty(shaderGraphAsm, graph, "FlameTexture", "_FlameTexture");
        var distortionNoiseProp = AddTextureProperty(shaderGraphAsm, graph, "DistortionNoise", "_DistortionNoise");

        var flameColorAProp = AddColorProperty(shaderGraphAsm, graph, "FlameColorA", "_FlameColorA", new Color(1f, 0.45f, 0.08f, 1f));
        var flameColorBProp = AddColorProperty(shaderGraphAsm, graph, "FlameColorB", "_FlameColorB", new Color(1f, 0.9f, 0.55f, 1f));

        var flameSpeedProp = AddFloatProperty(shaderGraphAsm, graph, "FlameSpeed", "_FlameSpeed", -0.18f);
        var flameScaleProp = AddFloatProperty(shaderGraphAsm, graph, "FlameScale", "_FlameScale", 1.2f);
        var noiseScaleProp = AddFloatProperty(shaderGraphAsm, graph, "NoiseScale", "_NoiseScale", 3.5f);
        var noiseSpeedProp = AddFloatProperty(shaderGraphAsm, graph, "NoiseSpeed", "_NoiseSpeed", -0.12f);
        var distortXProp = AddFloatProperty(shaderGraphAsm, graph, "DistortX", "_DistortX", 0.06f);
        var distortYProp = AddFloatProperty(shaderGraphAsm, graph, "DistortY", "_DistortY", 0.025f);
        var heightStartProp = AddFloatProperty(shaderGraphAsm, graph, "HeightStart", "_HeightStart", 0.28f);
        var heightEndProp = AddFloatProperty(shaderGraphAsm, graph, "HeightEnd", "_HeightEnd", 0.72f);
        var opacityProp = AddFloatProperty(shaderGraphAsm, graph, "Opacity", "_Opacity", 0.85f);

        nodes["uvText"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.UVNode", -1950, -500);
        nodes["textMaskProperty"] = AddPropertyNode(graph, propertyNodeType, textMaskProp, -1700, -500);
        nodes["textMaskSample"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.SampleTexture2DNode", -1450, -580);
        nodes["textMaskSplit"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.SplitNode", -1120, -580);

        nodes["uvHeight"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.UVNode", -1950, -120);
        nodes["heightSplit"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.SplitNode", -1710, -120);
        nodes["heightStartProperty"] = AddPropertyNode(graph, propertyNodeType, heightStartProp, -1710, 70);
        nodes["heightEndProperty"] = AddPropertyNode(graph, propertyNodeType, heightEndProp, -1710, 140);
        nodes["heightMask"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.SmoothstepNode", -1400, -60);

        nodes["uvNoise"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.UVNode", -1950, 380);
        nodes["noiseScaleProperty"] = AddPropertyNode(graph, propertyNodeType, noiseScaleProp, -1960, 530);
        nodes["noiseScaleCombine"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.CombineNode", -1700, 500);
        nodes["noiseTilingOffset"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.TilingAndOffsetNode", -1410, 340);
        nodes["timeNoise"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.TimeNode", -1720, 760);
        nodes["noiseSpeedProperty"] = AddPropertyNode(graph, propertyNodeType, noiseSpeedProp, -1720, 900);
        nodes["noiseSpeedMultiply"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.MultiplyNode", -1420, 820);
        nodes["noiseScrollVector"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.Vector2Node", -1120, 820);
        nodes["noiseUVAdd"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.AddNode", -860, 500);
        nodes["distortionNoiseProperty"] = AddPropertyNode(graph, propertyNodeType, distortionNoiseProp, -1110, 260);
        nodes["distortionNoiseSample"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.SampleTexture2DNode", -560, 360);
        nodes["distortionNoiseSplit"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.SplitNode", -230, 360);

        nodes["halfNode"] = AddFloatNode(graph, shaderGraphAsm, 0.5f, -230, 620);
        nodes["centeredNoise"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.SubtractNode", 40, 430);

        nodes["distortXProperty"] = AddPropertyNode(graph, propertyNodeType, distortXProp, 20, 760);
        nodes["distortYProperty"] = AddPropertyNode(graph, propertyNodeType, distortYProp, 20, 850);
        nodes["distortXMultiply"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.MultiplyNode", 300, 700);
        nodes["distortYMultiply"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.MultiplyNode", 300, 860);
        nodes["distortVector"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.Vector2Node", 620, 760);

        nodes["timeFlame"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.TimeNode", -260, 1080);
        nodes["flameSpeedProperty"] = AddPropertyNode(graph, propertyNodeType, flameSpeedProp, -260, 1230);
        nodes["flameSpeedMultiply"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.MultiplyNode", 40, 1120);
        nodes["scrollVector"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.Vector2Node", 360, 1120);
        nodes["uvFlame"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.UVNode", 300, 390);
        nodes["flameUVAddA"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.AddNode", 920, 620);
        nodes["flameUVAddB"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.AddNode", 1230, 780);

        nodes["flameScaleProperty"] = AddPropertyNode(graph, propertyNodeType, flameScaleProp, 1210, 1060);
        nodes["flameScaleCombine"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.CombineNode", 1460, 1030);
        nodes["flameScaleTilingOffset"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.TilingAndOffsetNode", 1730, 820);
        nodes["flameTextureProperty"] = AddPropertyNode(graph, propertyNodeType, flameTextureProp, 1730, 600);
        nodes["flameTextureSample"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.SampleTexture2DNode", 2060, 680);
        nodes["flameTextureSplit"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.SplitNode", 2390, 680);

        nodes["flameColorAProperty"] = AddPropertyNode(graph, propertyNodeType, flameColorAProp, 2060, 1050);
        nodes["flameColorBProperty"] = AddPropertyNode(graph, propertyNodeType, flameColorBProp, 2060, 1140);
        nodes["flameColorLerp"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.LerpNode", 2390, 1020);

        nodes["alphaMulText"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.MultiplyNode", 2720, 600);
        nodes["alphaMulHeight"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.MultiplyNode", 3020, 600);
        nodes["opacityProperty"] = AddPropertyNode(graph, propertyNodeType, opacityProp, 3020, 860);
        nodes["alphaMulOpacity"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.MultiplyNode", 3320, 650);
        nodes["finalColorMultiply"] = AddNode(graph, shaderGraphAsm, "UnityEditor.ShaderGraph.MultiplyNode", 3020, 1080);

        Connect(graph, nodes["uvText"], 0, nodes["textMaskSample"], 2);
        Connect(graph, nodes["textMaskProperty"], 0, nodes["textMaskSample"], 1);
        Connect(graph, nodes["textMaskSample"], 0, nodes["textMaskSplit"], 0);

        Connect(graph, nodes["uvHeight"], 0, nodes["heightSplit"], 0);
        Connect(graph, nodes["heightStartProperty"], 0, nodes["heightMask"], 0);
        Connect(graph, nodes["heightEndProperty"], 0, nodes["heightMask"], 1);
        Connect(graph, nodes["heightSplit"], 2, nodes["heightMask"], 2);

        Connect(graph, nodes["noiseScaleProperty"], 0, nodes["noiseScaleCombine"], 0);
        Connect(graph, nodes["noiseScaleProperty"], 0, nodes["noiseScaleCombine"], 1);
        Connect(graph, nodes["uvNoise"], 0, nodes["noiseTilingOffset"], 0);
        Connect(graph, nodes["noiseScaleCombine"], 6, nodes["noiseTilingOffset"], 1);
        Connect(graph, nodes["timeNoise"], 0, nodes["noiseSpeedMultiply"], 0);
        Connect(graph, nodes["noiseSpeedProperty"], 0, nodes["noiseSpeedMultiply"], 1);
        Connect(graph, nodes["noiseSpeedMultiply"], 2, nodes["noiseScrollVector"], 2);
        Connect(graph, nodes["noiseTilingOffset"], 3, nodes["noiseUVAdd"], 0);
        Connect(graph, nodes["noiseScrollVector"], 0, nodes["noiseUVAdd"], 1);
        Connect(graph, nodes["distortionNoiseProperty"], 0, nodes["distortionNoiseSample"], 1);
        Connect(graph, nodes["noiseUVAdd"], 2, nodes["distortionNoiseSample"], 2);
        Connect(graph, nodes["distortionNoiseSample"], 0, nodes["distortionNoiseSplit"], 0);

        Connect(graph, nodes["distortionNoiseSplit"], 1, nodes["centeredNoise"], 0);
        Connect(graph, nodes["halfNode"], 0, nodes["centeredNoise"], 1);

        Connect(graph, nodes["centeredNoise"], 2, nodes["distortXMultiply"], 0);
        Connect(graph, nodes["distortXProperty"], 0, nodes["distortXMultiply"], 1);
        Connect(graph, nodes["centeredNoise"], 2, nodes["distortYMultiply"], 0);
        Connect(graph, nodes["distortYProperty"], 0, nodes["distortYMultiply"], 1);
        Connect(graph, nodes["distortXMultiply"], 2, nodes["distortVector"], 1);
        Connect(graph, nodes["distortYMultiply"], 2, nodes["distortVector"], 2);

        Connect(graph, nodes["timeFlame"], 0, nodes["flameSpeedMultiply"], 0);
        Connect(graph, nodes["flameSpeedProperty"], 0, nodes["flameSpeedMultiply"], 1);
        Connect(graph, nodes["flameSpeedMultiply"], 2, nodes["scrollVector"], 2);

        Connect(graph, nodes["uvFlame"], 0, nodes["flameUVAddA"], 0);
        Connect(graph, nodes["distortVector"], 0, nodes["flameUVAddA"], 1);
        Connect(graph, nodes["flameUVAddA"], 2, nodes["flameUVAddB"], 0);
        Connect(graph, nodes["scrollVector"], 0, nodes["flameUVAddB"], 1);

        Connect(graph, nodes["flameScaleProperty"], 0, nodes["flameScaleCombine"], 0);
        Connect(graph, nodes["flameScaleProperty"], 0, nodes["flameScaleCombine"], 1);
        Connect(graph, nodes["flameUVAddB"], 2, nodes["flameScaleTilingOffset"], 0);
        Connect(graph, nodes["flameScaleCombine"], 6, nodes["flameScaleTilingOffset"], 1);
        Connect(graph, nodes["flameTextureProperty"], 0, nodes["flameTextureSample"], 1);
        Connect(graph, nodes["flameScaleTilingOffset"], 3, nodes["flameTextureSample"], 2);
        Connect(graph, nodes["flameTextureSample"], 0, nodes["flameTextureSplit"], 0);

        Connect(graph, nodes["flameColorAProperty"], 0, nodes["flameColorLerp"], 0);
        Connect(graph, nodes["flameColorBProperty"], 0, nodes["flameColorLerp"], 1);
        Connect(graph, nodes["flameTextureSplit"], 1, nodes["flameColorLerp"], 2);

        Connect(graph, nodes["flameTextureSplit"], 1, nodes["alphaMulText"], 0);
        Connect(graph, nodes["textMaskSplit"], 4, nodes["alphaMulText"], 1);
        Connect(graph, nodes["alphaMulText"], 2, nodes["alphaMulHeight"], 0);
        Connect(graph, nodes["heightMask"], 3, nodes["alphaMulHeight"], 1);
        Connect(graph, nodes["alphaMulHeight"], 2, nodes["alphaMulOpacity"], 0);
        Connect(graph, nodes["opacityProperty"], 0, nodes["alphaMulOpacity"], 1);

        Connect(graph, nodes["flameColorLerp"], 3, nodes["finalColorMultiply"], 0);
        Connect(graph, nodes["alphaMulOpacity"], 2, nodes["finalColorMultiply"], 1);

        var baseColorBlock = FindBlockNode(graph, blockNodeType, "SurfaceDescription.BaseColor");
        var alphaBlock = FindBlockNode(graph, blockNodeType, "SurfaceDescription.Alpha");
        Connect(graph, nodes["finalColorMultiply"], 2, baseColorBlock, 0);
        Connect(graph, nodes["alphaMulOpacity"], 2, alphaBlock, 0);

        Invoke(graph, "ValidateGraph");
        Directory.CreateDirectory(Path.GetDirectoryName(GraphPath)!);
        graphUtilType.GetMethod("WriteShaderGraphToDisk", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { GraphPath, graph });

        AssetDatabase.ImportAsset(GraphPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();

        var shader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
        if (shader == null)
        {
            throw new Exception("Shader import failed for fire_3.shadergraph");
        }

        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        material.SetTexture("_TextMask", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Projects/Playrix_VFX_Prog/Textures/text_mask.png"));
        material.SetTexture("_FlameTexture", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Projects/Playrix_VFX_Prog/Textures/flame_pattern_1.png"));
        material.SetTexture("_DistortionNoise", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Projects/Playrix_VFX_Prog/Textures/noise_a.png"));
        material.SetColor("_FlameColorA", new Color(1f, 0.45f, 0.08f, 1f));
        material.SetColor("_FlameColorB", new Color(1f, 0.9f, 0.55f, 1f));
        material.SetFloat("_FlameSpeed", -0.18f);
        material.SetFloat("_FlameScale", 1.2f);
        material.SetFloat("_NoiseScale", 3.5f);
        material.SetFloat("_NoiseSpeed", -0.12f);
        material.SetFloat("_DistortX", 0.06f);
        material.SetFloat("_DistortY", 0.025f);
        material.SetFloat("_HeightStart", 0.28f);
        material.SetFloat("_HeightEnd", 0.72f);
        material.SetFloat("_Opacity", 0.85f);

        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        Debug.Log("fire_3 Shader Graph generated successfully");
    }

    private static object AddTextureProperty(Assembly shaderGraphAsm, object graph, string displayName, string referenceName)
    {
        var type = shaderGraphAsm.GetType("UnityEditor.ShaderGraph.Internal.Texture2DShaderProperty", true)!;
        var prop = Activator.CreateInstance(type, true)!;
        SetProperty(prop, "displayName", displayName);
        SetProperty(prop, "value", new SerializableTexture());
        SetProperty(prop, "defaultType", Enum.Parse(type.GetNestedType("DefaultType")!, "White"));
        SetReferenceName(prop, referenceName);
        Invoke(graph, "AddGraphInput", prop, -1);
        return prop;
    }

    private static object AddColorProperty(Assembly shaderGraphAsm, object graph, string displayName, string referenceName, Color color)
    {
        var type = shaderGraphAsm.GetType("UnityEditor.ShaderGraph.Internal.ColorShaderProperty", true)!;
        var prop = Activator.CreateInstance(type, true)!;
        SetProperty(prop, "displayName", displayName);
        SetProperty(prop, "value", color);
        SetProperty(prop, "colorMode", ColorMode.Default);
        SetReferenceName(prop, referenceName);
        Invoke(graph, "AddGraphInput", prop, -1);
        return prop;
    }

    private static object AddFloatProperty(Assembly shaderGraphAsm, object graph, string displayName, string referenceName, float value)
    {
        var type = shaderGraphAsm.GetType("UnityEditor.ShaderGraph.Internal.Vector1ShaderProperty", true)!;
        var prop = Activator.CreateInstance(type, true)!;
        SetProperty(prop, "displayName", displayName);
        SetProperty(prop, "value", value);
        SetReferenceName(prop, referenceName);
        Invoke(graph, "AddGraphInput", prop, -1);
        return prop;
    }

    private static object AddPropertyNode(object graph, Type propertyNodeType, object property, float x, float y)
    {
        var node = Activator.CreateInstance(propertyNodeType, true)!;
        propertyNodeType.GetProperty("property", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(node, property);
        AddNodeToGraph(graph, node, x, y);
        return node;
    }

    private static object AddFloatNode(object graph, Assembly shaderGraphAsm, float value, float x, float y)
    {
        var vector1NodeType = shaderGraphAsm.GetType("UnityEditor.ShaderGraph.Vector1Node", true);
        var node = Activator.CreateInstance(vector1NodeType!, true)!;
        var findInputSlot = vector1NodeType!.GetMethod("FindInputSlot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .MakeGenericMethod(shaderGraphAsm.GetType("UnityEditor.ShaderGraph.Vector1MaterialSlot", true)!);
        var slot = findInputSlot.Invoke(node, new object[] { 1 });
        slot!.GetType().GetProperty("value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(slot, value);
        AddNodeToGraph(graph, node, x, y);
        return node;
    }

    private static object AddNode(object graph, Assembly asm, string typeName, float x, float y)
    {
        var node = Activator.CreateInstance(asm.GetType(typeName, true)!, true)!;
        AddNodeToGraph(graph, node, x, y);
        return node;
    }

    private static void AddNodeToGraph(object graph, object node, float x, float y)
    {
        Invoke(graph, "AddNode", node);
        SetNodePosition(node, x, y);
    }

    private static void SetNodePosition(object node, float x, float y)
    {
        var drawStateProp = node.GetType().GetProperty("drawState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        var drawState = drawStateProp.GetValue(node);
        if (drawState == null)
        {
            return;
        }

        var positionField = drawState.GetType().GetField("m_Position", BindingFlags.Instance | BindingFlags.NonPublic);
        if (positionField != null)
        {
            var current = positionField.GetValue(drawState) is Rect rect ? rect : new Rect();
            current.position = new Vector2(x, y);
            current.size = new Vector2(Mathf.Max(current.width, 200f), Mathf.Max(current.height, 80f));
            positionField.SetValue(drawState, current);
            drawStateProp.SetValue(node, drawState);
        }
    }

    private static void Connect(object graph, object fromNode, int fromSlotId, object toNode, int toSlotId)
    {
        var slotRefType = fromNode.GetType().Assembly.GetType("UnityEditor.Graphing.SlotReference", true)!;
        var fromSlotRef = Activator.CreateInstance(slotRefType, fromNode, fromSlotId)!;
        var toSlotRef = Activator.CreateInstance(slotRefType, toNode, toSlotId)!;
        Invoke(graph, "Connect", fromSlotRef, toSlotRef);
    }

    private static object FindBlockNode(object graph, Type blockNodeType, string serializedDescriptor)
    {
        var getNodes = graph.GetType().GetMethod("GetNodes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .MakeGenericMethod(blockNodeType);
        var blocks = (IEnumerable)getNodes.Invoke(graph, null)!;
        foreach (var block in blocks)
        {
            var value = blockNodeType.GetProperty("serializedDescriptor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(block) as string;
            if (value == serializedDescriptor)
            {
                return block;
            }
        }

        throw new Exception($"Block node not found: {serializedDescriptor}");
    }

    private static object GetStaticPropertyValue(Type type, string path)
    {
        object current = type;
        foreach (var part in path.Split('.'))
        {
            var nextType = current as Type ?? current.GetType();
            var prop = nextType.GetProperty(part, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            current = prop!.GetValue(current is Type ? null : current)!;
        }

        return current;
    }

    private static object Invoke(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .First(m => m.Name == methodName && m.GetParameters().Length == args.Length);
        return method.Invoke(target, args);
    }

    private static object InvokeStatic(Type type, string methodName, params object[] args)
    {
        var method = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .First(m => m.Name == methodName && m.GetParameters().Length == args.Length);
        return method.Invoke(null, args);
    }

    private static void SetProperty(object target, string propertyName, object value)
    {
        target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(target, value);
    }

    private static void SetReferenceName(object property, string referenceName)
    {
        var field = FindFieldRecursive(property.GetType(), "m_DefaultReferenceName");
        field!.SetValue(property, referenceName);
        var versionField = FindFieldRecursive(property.GetType(), "m_DefaultRefNameVersion");
        versionField!.SetValue(property, 1);
        var generatedField = FindFieldRecursive(property.GetType(), "m_RefNameGeneratedByDisplayName");
        generatedField!.SetValue(property, property.GetType().GetProperty("displayName")!.GetValue(property));
    }

    private static FieldInfo? FindFieldRecursive(Type type, string fieldName)
    {
        while (type != null)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null)
            {
                return field;
            }

            type = type.BaseType!;
        }

        return null;
    }
}
