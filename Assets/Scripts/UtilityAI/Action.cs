using UnityEngine;

namespace UtilityAI.Core 
{
    public abstract class Action : ScriptableObject
    {
        public Consideration[] considerations;
        private float _score;
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

        }
        public abstract void Execute(Context context);
    }
}



