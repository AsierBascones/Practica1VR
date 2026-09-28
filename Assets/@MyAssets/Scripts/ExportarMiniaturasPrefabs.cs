using UnityEngine;
using UnityEditor;
using System.IO;

public class ExportarMiniaturasPrefabs : Editor
{
    [MenuItem("Assets/Generar Sprite de este Prefab", true)]
    private static bool ValidarSeleccion()
    {
        // Solo habilitar si lo seleccionado es un Prefab o GameObject
        return Selection.activeGameObject != null;
    }

    [MenuItem("Assets/Generar Sprite de este Prefab")]
    private static void GenerarMiniatura()
    {
        GameObject prefab = Selection.activeGameObject;

        // Obtener la textura de previsualización que usa Unity internamente
        Texture2D preview = AssetPreview.GetAssetPreview(prefab);

        if (preview == null)
        {
            EditorUtility.DisplayDialog("Aviso", "La vista previa aún no está cargada. Inténtalo de nuevo en un segundo.", "OK");
            return;
        }

        // Crear una copia legible de la textura
        RenderTexture rt = RenderTexture.GetTemporary(preview.width, preview.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(preview, rt);

        RenderTexture previo = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D texturaExportar = new Texture2D(preview.width, preview.height, TextureFormat.RGBA32, false);
        texturaExportar.ReadPixels(new Rect(0, 0, preview.width, preview.height), 0, 0);
        texturaExportar.Apply();

        RenderTexture.active = previo;
        RenderTexture.ReleaseTemporary(rt);

        // Guardar como PNG en Assets/@MyAssets/Images
        string carpetaDestino = "Assets/@MyAssets/Images";
        if (!Directory.Exists(carpetaDestino))
        {
            Directory.CreateDirectory(carpetaDestino);
        }

        string ruta = Path.Combine(carpetaDestino, prefab.name + "_Icon.png");
        byte[] bytes = texturaExportar.EncodeToPNG();
        File.WriteAllBytes(ruta, bytes);

        AssetDatabase.Refresh();

        // Configurar automáticamente la textura importada como Sprite (2D and UI)
        TextureImporter importer = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.SaveAndReimport();
        }

        Debug.Log("Sprite generado con éxito en: " + ruta);
    }
}