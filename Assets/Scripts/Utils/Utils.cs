using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Util.Core 
{
    public static class Utils
    {       
        public static async UniTaskVoid DelayCall(float seconds, System.Action action, PlayerLoopTiming timing = PlayerLoopTiming.Update, DelayType delayType = DelayType.DeltaTime, CancellationToken token = default)
        {
            int delayMiliseconds = Mathf.RoundToInt(seconds * 1000);
            try
            {
                await UniTask.Delay(delayMiliseconds, delayType, delayTiming: timing, cancellationToken: token);
                action?.Invoke();
            }
            catch (OperationCanceledException e)
            {
                Debug.Log($"DelayCall was cancelled: {e.Message}");
            }
        }

        public static async UniTaskVoid Delay1Frame(System.Action action, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken token = default)
        {
            try
            {
                await UniTask.Yield(timing, token);
                action?.Invoke();
            }
            catch (OperationCanceledException e)
            {
                Debug.Log($"Delay1Frame was cancelled: {e.Message}");
            }
        }
        public static async UniTask IntervalLoop(
            Action action,
            int intervalMs,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            DelayType delayType = DelayType.DeltaTime,
            CancellationToken token = default
            )
        {
            while (!token.IsCancellationRequested)
            {
                action?.Invoke();

                await UniTask.Delay(
                    intervalMs,
                    delayType: delayType,
                    delayTiming: timing,
                    cancellationToken: token);
            }
        }


        /// <summary>
        /// Hash một Vector3Int thành int để làm key
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static int ToKey(this Vector3Int v)
        {
            return (v.x & 0x3FF) << 20 | (v.y & 0x3FF) << 10 | (v.z & 0x3FF);
        }

        /// <summary>
        /// Unserialize key Int thành Vector3Int
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Vector3Int FromKey(this int key)
        {
            int x = (key >> 20) & 0x3FF;
            int y = (key >> 10) & 0x3FF;
            int z = key & 0x3FF;
            return new Vector3Int(x, y, z);
        }

        /// <summary>
        /// Lấy component nếu có, nếu chưa thì tự động Add và trả về.
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        /// Lấy component nếu có, nếu chưa thì tự động Add và trả về.
        /// </summary>
        public static T GetOrAddComponent<T>(this Component component) where T : Component
        {
            return component.gameObject.GetOrAddComponent<T>();
        }



        public static int RandomWeight(float[] weight)
        {
            float totalWeight = weight.Sum();
            float randomValue = UnityEngine.Random.value  * totalWeight;
            float upto = 0f;
            for (int i = 0; i < weight.Length; i ++)
            {
                upto += weight[i];
                if (randomValue <= upto)
                {
                    return i;
                }
            }
            return weight.Length - 1;
        }

    }
}



