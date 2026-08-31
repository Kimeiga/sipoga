using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sipoga.Tactics
{
    public static class TacticalPrototypeVisual
    {
        private static readonly Dictionary<string, Material> Materials =
            new Dictionary<string, Material>();

        public static Material GetMaterial(Color color, bool unlit)
        {
            string key = ColorUtility.ToHtmlStringRGBA(color) + (unlit ? "-unlit" : "-lit");
            Material existing;
            if (Materials.TryGetValue(key, out existing) && existing != null)
            {
                return existing;
            }

            Shader shader = FindCompatibleShader(unlit);
            if (shader == null)
            {
                shader = Shader.Find("Hidden/InternalErrorShader");
            }

            Material material = new Material(shader);
            material.name = "Runtime Tactical Material " + key;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_UnlitColor"))
            {
                material.SetColor("_UnlitColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_EmissiveColor"))
            {
                material.SetColor("_EmissiveColor", color * 0.12f);
            }

            Materials[key] = material;
            return material;
        }

        public static void ConfigurePipelineCamera(GameObject cameraObject)
        {
            string pipelineName = GetActivePipelineName();
            if (pipelineName.IndexOf("HDRenderPipeline", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                TryAddLoadedComponent(
                    cameraObject,
                    "UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData");
            }
            else if (pipelineName.IndexOf("Universal", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                TryAddLoadedComponent(
                    cameraObject,
                    "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData");
            }
        }

        public static void ConfigurePipelineLight(GameObject lightObject)
        {
            string pipelineName = GetActivePipelineName();
            if (pipelineName.IndexOf("HDRenderPipeline", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                TryAddLoadedComponent(
                    lightObject,
                    "UnityEngine.Rendering.HighDefinition.HDAdditionalLightData");
            }
            else if (pipelineName.IndexOf("Universal", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                TryAddLoadedComponent(
                    lightObject,
                    "UnityEngine.Rendering.Universal.UniversalAdditionalLightData");
            }
        }

        private static string GetActivePipelineName()
        {
            RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
            return pipeline != null ? pipeline.GetType().Name : string.Empty;
        }

        private static void TryAddLoadedComponent(GameObject target, string fullTypeName)
        {
            if (target == null)
            {
                return;
            }

            System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                System.Type type = assemblies[i].GetType(fullTypeName, false);
                if (type == null || !typeof(Component).IsAssignableFrom(type))
                {
                    continue;
                }

                if (target.GetComponent(type) == null)
                {
                    target.AddComponent(type);
                }

                return;
            }
        }

        private static Shader FindCompatibleShader(bool unlit)
        {
            RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
            string pipelineName = pipeline != null ? pipeline.GetType().Name : string.Empty;

            if (pipelineName.IndexOf("HDRenderPipeline", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Shader hdrp = Shader.Find(unlit ? "HDRP/Unlit" : "HDRP/Lit");
                if (hdrp != null)
                {
                    return hdrp;
                }
            }

            if (pipelineName.IndexOf("Universal", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Shader urp = Shader.Find(
                    unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
                if (urp != null)
                {
                    return urp;
                }
            }

            string[] fallbackNames = unlit
                ? new[]
                {
                    "HDRP/Unlit",
                    "Universal Render Pipeline/Unlit",
                    "Unlit/Color",
                    "Sprites/Default"
                }
                : new[]
                {
                    "HDRP/Lit",
                    "Universal Render Pipeline/Lit",
                    "Standard",
                    "Unlit/Color"
                };

            for (int i = 0; i < fallbackNames.Length; i++)
            {
                Shader candidate = Shader.Find(fallbackNames[i]);
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return null;
        }

        public static GameObject CreateBlock(
            string name,
            Vector3 position,
            Vector3 scale,
            Color color,
            Transform parent,
            bool collider)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.position = position;
            block.transform.localScale = scale;
            Renderer renderer = block.GetComponent<Renderer>();
            renderer.sharedMaterial = GetMaterial(color, true);

            if (!collider)
            {
                Collider blockCollider = block.GetComponent<Collider>();
                if (blockCollider != null)
                {
                    Object.Destroy(blockCollider);
                }
            }

            return block;
        }

        public static GameObject CreateCylinder(
            string name,
            Vector3 position,
            Vector3 scale,
            Color color,
            Transform parent,
            bool collider)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.position = position;
            cylinder.transform.localScale = scale;
            Renderer renderer = cylinder.GetComponent<Renderer>();
            renderer.sharedMaterial = GetMaterial(color, true);

            if (!collider)
            {
                Collider cylinderCollider = cylinder.GetComponent<Collider>();
                if (cylinderCollider != null)
                {
                    Object.Destroy(cylinderCollider);
                }
            }

            return cylinder;
        }

        public static TextMesh CreateWorldLabel(
            string text,
            Vector3 position,
            float characterSize,
            Color color,
            Transform parent)
        {
            GameObject labelObject = new GameObject("Label " + text);
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.position = position;
            TextMesh textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 64;
            textMesh.characterSize = characterSize;
            textMesh.color = color;
            textMesh.fontStyle = FontStyle.Bold;
            labelObject.AddComponent<TacticalBillboard>();
            return textMesh;
        }

        public static LineRenderer CreateLine(
            string name,
            float width,
            Color color,
            Transform parent)
        {
            GameObject lineObject = new GameObject(name);
            lineObject.transform.SetParent(parent, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = width;
            line.endWidth = width;
            line.numCapVertices = 4;
            line.numCornerVertices = 2;
            line.sharedMaterial = GetMaterial(color, true);
            line.startColor = color;
            line.endColor = color;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }
    }

    public sealed class TacticalBillboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            transform.rotation = camera.transform.rotation;
        }
    }
}
