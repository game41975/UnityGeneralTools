using UnityEngine;
using Addressable;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using NUnit.Framework;
using System.Collections.Generic;
//using UnityEngine.UIElements;

public class AddressableTestScene : MonoBehaviour
{
    [SerializeField] AddressableAssetManager assetManager;
    [SerializeField] RawImage rawImage;

    private void Start()
    {
        var handle = Addressables.LoadAssetAsync<Texture2D>("Test");
        handle.Completed += _handle =>
        {
            if (_handle.Result != null)
            {
                rawImage.texture = _handle.Result;
            }
        };

    }
}
