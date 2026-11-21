using System.Collections.Generic;

namespace SilentCastle.MonsterAI
{
    /// <summary>Hệ số nhân tạm thời. 1 = không đổi.</summary>
    public struct StatMod
    {
        public float MoveSpeed;
        public float AttackSpeed;
        public float Damage;

        public static StatMod One => new StatMod { MoveSpeed = 1f, AttackSpeed = 1f, Damage = 1f };
    }

    /// <summary>
    /// Tập hợp các modifier theo nguồn (skill, buff, debuff...). Nhiều nguồn nhân chồng lên nhau.
    /// Skill mới chỉ cần Set(this, mod) / Remove(this).
    /// </summary>
    public class StatModifierSet
    {
        readonly Dictionary<object, StatMod> _mods = new Dictionary<object, StatMod>();
        bool _dirty = true;
        float _move = 1f, _atk = 1f, _dmg = 1f;

        public float MoveSpeedMul { get { Recalc(); return _move; } }
        public float AttackSpeedMul { get { Recalc(); return _atk; } }
        public float DamageMul { get { Recalc(); return _dmg; } }

        public void Set(object source, StatMod mod) { _mods[source] = mod; _dirty = true; }
        public void Remove(object source) { if (_mods.Remove(source)) _dirty = true; }
        public void Clear() { _mods.Clear(); _dirty = true; }

        void Recalc()
        {
            if (!_dirty) return;
            _move = _atk = _dmg = 1f;
            foreach (var m in _mods.Values)
            {
                _move *= m.MoveSpeed;
                _atk *= m.AttackSpeed;
                _dmg *= m.Damage;
            }
            _dirty = false;
        }
    }
}
