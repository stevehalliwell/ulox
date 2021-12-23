using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor.AssetImporters;

namespace ULox.Unity.Editor
{
    [ScriptedImporter(1, "ulox")]
    public class ULoxScriptToTextAssetScriptedImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            TextAsset fileAsTextAsset = new TextAsset(File.ReadAllText(ctx.assetPath));
            ctx.AddObjectToAsset("main", fileAsTextAsset);
            ctx.SetMainObject(fileAsTextAsset);
        }
    }
}
#endif