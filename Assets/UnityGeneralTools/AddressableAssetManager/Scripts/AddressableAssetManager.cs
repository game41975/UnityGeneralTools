using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Addressable
{
    public class AddressableAssetManager : MonoBehaviour
    {
        public class LoadedAssetHandleParam
        {
            public string assetKey;
            public AsyncOperationHandle m_handle;
        }

        private List<LoadedAssetHandleParam> m_cachedAssetHandles = new List<LoadedAssetHandleParam>();

        public void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
        }

        /// <summary>
        /// アセット読み込み
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="onLoadSuccessded"></param>
        /// <param name="onLoadFailed"></param>
        public void LoadAsset<T>(string key,System.Action<T> onLoadSuccessded,System.Action onLoadFailed)where T : Object
        {
            var handle =  Addressables.LoadAssetAsync<T>(key);
            handle.Completed += _handle => 
            {
                if (_handle.Result != null)
                {
                    onLoadSuccessded?.Invoke(_handle.Result);
                    m_cachedAssetHandles.Add(new LoadedAssetHandleParam() 
                    {
                        assetKey = key,
                        m_handle = handle,
                    });
                }
                else
                {
                    onLoadFailed?.Invoke();
                }
            };
        }

        /// <summary>
        /// アセット読み込み(非同期)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="onLoadSuccessded"></param>
        /// <param name="onLoadFailed"></param>
        /// <returns></returns>
        public IEnumerator LoadAssetAsync<T>(string key, System.Action<T> onLoadSuccessded, System.Action onLoadFailed) where T : Object
        {
            var operation = Addressables.LoadAssetAsync<T>(key);

            if (!operation.IsDone)
            {
                yield return operation;
            }

            if(operation.Status == AsyncOperationStatus.Succeeded)
            {
                onLoadSuccessded?.Invoke(operation.Result);
            }
            else
            {
                onLoadFailed.Invoke();
                Addressables.Release(operation);
            }

            yield break;
        }

        /// <summary>
        /// 読み込み済みのすべてのハンドルを削除
        /// </summary>
        public void ReleaseLoadedAssetHandleAll()
        {
            for(int i = m_cachedAssetHandles.Count -1; i >= 0; i--)
            {
                var param = m_cachedAssetHandles[i];
                if (param.m_handle.IsValid())
                {
                    Addressables.Release(param.m_handle);
                }
                m_cachedAssetHandles.RemoveAt(i);
            }
            m_cachedAssetHandles.Clear();
        }

        public void OnDestroy()
        {
            ReleaseLoadedAssetHandleAll();
        }
    }
}

