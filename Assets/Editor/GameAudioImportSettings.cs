using UnityEditor;
using UnityEngine;

public sealed class GameAudioImportSettings : AssetPostprocessor
{
    private void OnPreprocessAudio()
    {
        if(!assetPath.StartsWith("Assets/Resources/GameAudio/"))return;
        var importer=(AudioImporter)assetImporter;
        bool music=assetPath.Contains("/Music/");
        var settings=importer.defaultSampleSettings;
        settings.loadType=music?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat=AudioCompressionFormat.Vorbis;
        settings.quality=music?.85f:.75f;
        settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
        settings.preloadAudioData=!music;
        importer.defaultSampleSettings=settings;
        importer.forceToMono=false;importer.loadInBackground=false;
    }
}
