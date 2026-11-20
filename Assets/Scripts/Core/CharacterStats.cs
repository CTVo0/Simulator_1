using System;
using UnityEngine;

namespace CombatSim.Core
{
    [Serializable]
    public class CharacterStats
    {
        public string Name;
        public int MaxHP;
        public int CurrentHP;
        public int CombatSkill;
        public int DefenseSkill;
        public int DamageMin;
        public int DamageMax;
        public float AccuracyDecay; // Tính theo điểm phần trăm (vd: 5 = giảm 5% mỗi đòn)
        public Color UnitColor;

        public bool IsDead => CurrentHP <= 0;

        public CharacterStats(string name, int maxHp, int combat, int defense, int dmgMin, int dmgMax, float decay, Color color)
        {
            Name = name;
            MaxHP = Mathf.Max(0, maxHp);
            CurrentHP = MaxHP;
            CombatSkill = Mathf.Max(0, combat);
            DefenseSkill = Mathf.Max(0, defense);
            DamageMin = Mathf.Max(0, dmgMin);
            DamageMax = Mathf.Max(DamageMin, dmgMax);
            AccuracyDecay = Mathf.Clamp(decay, 0f, 90f);
            UnitColor = color;
        }

        public void ResetHP()
        {
            CurrentHP = MaxHP;
        }
    }
}