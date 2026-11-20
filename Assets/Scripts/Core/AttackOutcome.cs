using UnityEngine;

namespace CombatSim.Core
{
    public struct AttackOutcome
    {
        public bool IsHit;
        public bool IsCritical;
        public float HitChanceUsed;
        public int BaseDamage;
        public int FinalDamage;
        public int TargetRemainingHP;
    }

    public static class CombatRules
    {
        public const float MinHitChance = 0.05f;
        public const float MaxHitChance = 0.95f;
        public const float CriticalRate = 0.10f;      // Hằng số 10%
        public const int CriticalMultiplier = 2;      // Sát thương chí mạng x2

        /// <summary>
        /// P = clamp((CombatSkill - DefenseSkill) / CombatSkill, 5%, 95%).
        /// Nếu CombatSkill <= 0, trả về 5%.
        /// </summary>
        public static float CalculateHitChance(int combatSkill, int defenseSkill)
        {
            if (combatSkill <= 0) return MinHitChance;
            float raw = (float)(combatSkill - defenseSkill) / combatSkill;
            return Mathf.Clamp(raw, MinHitChance, MaxHitChance);
        }

        /// <summary>
        /// Thực thi một đòn đánh đơn lẻ (có thể truyền xác suất đã suy giảm từ Attack x5).
        /// </summary>
        public static AttackOutcome ResolveAttack(CharacterStats attacker, CharacterStats target, float? hitChanceOverride = null)
        {
            float hitChance = hitChanceOverride ?? CalculateHitChance(attacker.CombatSkill, target.DefenseSkill);
            bool isHit = Random.value < hitChance;

            if (!isHit)
            {
                return new AttackOutcome
                {
                    IsHit = false,
                    IsCritical = false,
                    HitChanceUsed = hitChance,
                    BaseDamage = 0,
                    FinalDamage = 0,
                    TargetRemainingHP = target.CurrentHP
                };
            }

            // Random số nguyên trong khoảng [DamageMin, DamageMax]
            int baseDamage = Random.Range(attacker.DamageMin, attacker.DamageMax + 1);

            // Chỉ xét Critical sau khi đã đánh trúng
            bool isCrit = Random.value < CriticalRate;
            int finalDamage = isCrit ? baseDamage * CriticalMultiplier : baseDamage;

            target.CurrentHP = Mathf.Max(0, target.CurrentHP - finalDamage);

            return new AttackOutcome
            {
                IsHit = true,
                IsCritical = isCrit,
                HitChanceUsed = hitChance,
                BaseDamage = baseDamage,
                FinalDamage = finalDamage,
                TargetRemainingHP = target.CurrentHP
            };
        }
    }   
}