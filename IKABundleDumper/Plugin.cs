using BepInEx;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;
using IKA9nt.Encrypter;
using SeekableAesAssetBundle.Scripts;
using System.IO;

namespace IKABundleDumper
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BasePlugin
    {
        public static ManualLogSource dumperLog;
        public static string password;
        public static string assetBundlePath;
        public static string labelName;
        public static EncrypterKeyData KeyData;

        public override void Load()
        {
            dumperLog = Log;
            
            var harmony = Harmony.CreateAndPatchAll(typeof(Patches)); //Patch classes
        }

        public static void CopyStream(Il2CppSystem.IO.Stream input, Stream output)
        {
            //Make an Il2Cpp byte array for the buffer
            //A regular byte array just results in the entire file being null bytes
            UnhollowerBaseLib.Il2CppStructArray<byte> buffer = new UnhollowerBaseLib.Il2CppStructArray<byte>(8 * 1024);
            int len;
            while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, len);
            }
        }
    }

    public class Patches
    {
        //Patch the GetStream function in EncrypterCore so we can immediately dump the bundle when it gets loaded
        //This will hang the game for a decent amount of time
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EncrypterCore), "GetStream")]
        public static void streamPatch(ref SeekableAesStream __result, ref EncrypterKeyData KeyData, ref string labelName, ref string password, ref string assetBundlePath)
        {
            Plugin.dumperLog.LogInfo("AssetBundle password: " + password);
            Plugin.dumperLog.LogInfo("AssetBundle path: " + assetBundlePath);
            Plugin.dumperLog.LogInfo("Label name: " + labelName);

            var streamToCopy = __result;
            string decryptedFilePath = Path.GetDirectoryName(assetBundlePath) + "\\" + labelName + ".assetbundle";
            using (Stream file = File.Create(decryptedFilePath))
            {
                Plugin.dumperLog.LogInfo("Dumping to " + decryptedFilePath);
                Plugin.CopyStream(streamToCopy, file);
            }

            Plugin.dumperLog.LogInfo("Finished dumping");
        }
    }
}