using UnityEngine;

namespace UtilityAI.Core
{
    public abstract class Consideration : ScriptableObject
    {
        public string Name;
        private float _score;
        [SerializeField] protected AnimationCurve responseCurve;
        public float score
        {
            get => _score;
            set
            {
                this._score = Mathf.Clamp01(value);
            }
        }


        public virtual void Awake()
        {
            score = 0;
        }


        public abstract float ScoreConsideration(Context context);
    }

}

