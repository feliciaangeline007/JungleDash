// ProgressionManager.cs - Handles player progression, stage stars, upgrades, skins, missions, and daily rewards.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace JungleDash
{
    public class ProgressionManager
    {
        public int TotalCoins { get; private set; }
        public int BestDistance { get; private set; }
        public int UnlockedStage { get; private set; }
        public int SelectedSkin { get; private set; }

        // Upgrades
        public int ShieldLevel { get; private set; }
        public int MagnetLevel { get; private set; }
        public int SpeedLevel { get; private set; }
        public int FlyLevel { get; private set; }
        public int DoubleLevel { get; private set; }

        // Daily Rewards
        public int DailyStreak { get; private set; }
        public string LastDailyDate { get; private set; }
        public bool BonusShield { get; set; }
        public bool BonusMagnet { get; set; }
        public bool BonusFly { get; set; }

        // Missions
        public readonly List<Mission> Missions = new List<Mission>();

        // Stage Stars cache
        readonly Dictionary<int, int> stageStars = new Dictionary<int, int>();

        public void Load()
        {
            TotalCoins = PlayerPrefs.GetInt("jd_total_coins", 0);
            BestDistance = PlayerPrefs.GetInt("jd_best", 0);
            UnlockedStage = Mathf.Clamp(PlayerPrefs.GetInt("jd_level_unlocked", 1), 1, GameConfig.Stages.Length);
            SelectedSkin = Mathf.Clamp(PlayerPrefs.GetInt("jd_selected_skin", 0), 0, GameConfig.Skins.Length - 1);

            // Upgrades
            ShieldLevel = Mathf.Clamp(PlayerPrefs.GetInt("jd_upg_shield", 1), 1, GameConfig.MaxUpgradeLevel);
            MagnetLevel = Mathf.Clamp(PlayerPrefs.GetInt("jd_upg_magnet", 1), 1, GameConfig.MaxUpgradeLevel);
            SpeedLevel = Mathf.Clamp(PlayerPrefs.GetInt("jd_upg_speed", 1), 1, GameConfig.MaxUpgradeLevel);
            FlyLevel = Mathf.Clamp(PlayerPrefs.GetInt("jd_upg_fly", 1), 1, GameConfig.MaxUpgradeLevel);
            DoubleLevel = Mathf.Clamp(PlayerPrefs.GetInt("jd_upg_double", 1), 1, GameConfig.MaxUpgradeLevel);

            // Daily Login
            LastDailyDate = PlayerPrefs.GetString("jd_daily_last_date", "");
            DailyStreak = PlayerPrefs.GetInt("jd_daily_streak", 0);
            BonusShield = PlayerPrefs.GetInt("jd_bonus_shield", 0) == 1;
            BonusMagnet = PlayerPrefs.GetInt("jd_bonus_magnet", 0) == 1;
            BonusFly = PlayerPrefs.GetInt("jd_bonus_fly", 0) == 1;

            // Load Stars for each stage
            for (int i = 1; i <= GameConfig.Stages.Length; i++)
            {
                stageStars[i] = PlayerPrefs.GetInt("jd_stage_stars_" + i, 0);
            }

            // Ensure skin 0 is always unlocked
            PlayerPrefs.SetInt("jd_skin_unlocked_0", 1);

            InitMissions();
        }

        public void Save()
        {
            PlayerPrefs.SetInt("jd_total_coins", TotalCoins);
            PlayerPrefs.SetInt("jd_best", BestDistance);
            PlayerPrefs.SetInt("jd_level_unlocked", UnlockedStage);
            PlayerPrefs.SetInt("jd_selected_skin", SelectedSkin);

            PlayerPrefs.SetInt("jd_upg_shield", ShieldLevel);
            PlayerPrefs.SetInt("jd_upg_magnet", MagnetLevel);
            PlayerPrefs.SetInt("jd_upg_speed", SpeedLevel);
            PlayerPrefs.SetInt("jd_upg_fly", FlyLevel);
            PlayerPrefs.SetInt("jd_upg_double", DoubleLevel);

            PlayerPrefs.SetString("jd_daily_last_date", LastDailyDate);
            PlayerPrefs.SetInt("jd_daily_streak", DailyStreak);
            PlayerPrefs.SetInt("jd_bonus_shield", BonusShield ? 1 : 0);
            PlayerPrefs.SetInt("jd_bonus_magnet", BonusMagnet ? 1 : 0);
            PlayerPrefs.SetInt("jd_bonus_fly", BonusFly ? 1 : 0);

            SaveMissions();
            PlayerPrefs.Save();
        }

        public void AddCoins(int amount)
        {
            TotalCoins = Mathf.Max(0, TotalCoins + amount);
            PlayerPrefs.SetInt("jd_total_coins", TotalCoins);
        }

        public bool SpendCoins(int amount)
        {
            if (TotalCoins >= amount)
            {
                TotalCoins -= amount;
                PlayerPrefs.SetInt("jd_total_coins", TotalCoins);
                return true;
            }
            return false;
        }

        public bool UpdateBestDistance(int dist)
        {
            if (dist > BestDistance)
            {
                BestDistance = dist;
                PlayerPrefs.SetInt("jd_best", BestDistance);
                return true;
            }
            return false;
        }

        public int GetStageStars(int stageId)
        {
            if (stageStars.TryGetValue(stageId, out int stars)) return stars;
            return 0;
        }

        public void RecordStageClear(int stageId, int coinsCollected)
        {
            if (stageId < 1 || stageId > GameConfig.Stages.Length) return;
            LevelData stage = GameConfig.Stages[stageId - 1];

            int stars = 1;
            if (coinsCollected >= stage.star2Coins) stars = 2;
            if (coinsCollected >= stage.star3Coins) stars = 3;

            int prev = GetStageStars(stageId);
            if (stars > prev)
            {
                stageStars[stageId] = stars;
                PlayerPrefs.SetInt("jd_stage_stars_" + stageId, stars);
            }

            if (stageId == UnlockedStage && stageId < GameConfig.Stages.Length)
            {
                UnlockedStage = stageId + 1;
                PlayerPrefs.SetInt("jd_level_unlocked", UnlockedStage);
            }

            AddCoins(stage.coinReward);
            Save();
        }

        // Upgrades
        public int GetUpgradeLevel(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.Shield: return ShieldLevel;
                case UpgradeType.Magnet: return MagnetLevel;
                case UpgradeType.SpeedBoost: return SpeedLevel;
                case UpgradeType.Flight: return FlyLevel;
                case UpgradeType.DoubleScore: return DoubleLevel;
                default: return 1;
            }
        }

        public int GetUpgradeCost(UpgradeType type)
        {
            int lvl = GetUpgradeLevel(type);
            if (lvl >= GameConfig.MaxUpgradeLevel) return -1;
            return GameConfig.UpgradeCosts[lvl - 1];
        }

        public bool BuyUpgrade(UpgradeType type)
        {
            int cost = GetUpgradeCost(type);
            if (cost <= 0 || TotalCoins < cost) return false;

            if (SpendCoins(cost))
            {
                switch (type)
                {
                    case UpgradeType.Shield: ShieldLevel++; break;
                    case UpgradeType.Magnet: MagnetLevel++; break;
                    case UpgradeType.SpeedBoost: SpeedLevel++; break;
                    case UpgradeType.Flight: FlyLevel++; break;
                    case UpgradeType.DoubleScore: DoubleLevel++; break;
                }
                Save();
                return true;
            }
            return false;
        }

        public float GetPowerUpDuration(UpgradeType type)
        {
            int lvl = GetUpgradeLevel(type);
            float bonus = (lvl - 1) * 1.5f;
            switch (type)
            {
                case UpgradeType.Shield: return GameConfig.BaseShieldDuration + bonus;
                case UpgradeType.Magnet: return GameConfig.BaseMagnetDuration + bonus;
                case UpgradeType.SpeedBoost: return GameConfig.BaseSpeedDuration + bonus;
                case UpgradeType.Flight: return GameConfig.BaseFlightDuration + bonus;
                case UpgradeType.DoubleScore: return GameConfig.BaseDoubleDuration + bonus;
                default: return 8f;
            }
        }

        // Skins
        public bool IsSkinUnlocked(int skinId)
        {
            if (skinId == 0) return true;
            return PlayerPrefs.GetInt("jd_skin_unlocked_" + skinId, 0) == 1;
        }

        public bool SelectSkin(int skinId)
        {
            if (skinId < 0 || skinId >= GameConfig.Skins.Length) return false;
            if (IsSkinUnlocked(skinId))
            {
                SelectedSkin = skinId;
                PlayerPrefs.SetInt("jd_selected_skin", SelectedSkin);
                return true;
            }
            return false;
        }

        public bool BuySkin(int skinId)
        {
            if (skinId < 0 || skinId >= GameConfig.Skins.Length) return false;
            if (IsSkinUnlocked(skinId)) return false;

            int price = GameConfig.Skins[skinId].price;
            if (SpendCoins(price))
            {
                PlayerPrefs.SetInt("jd_skin_unlocked_" + skinId, 1);
                SelectedSkin = skinId;
                PlayerPrefs.SetInt("jd_selected_skin", SelectedSkin);
                Save();
                return true;
            }
            return false;
        }

        // Daily Login
        public bool CanClaimDailyReward()
        {
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            return LastDailyDate != today;
        }

        public string ClaimDailyReward()
        {
            if (!CanClaimDailyReward()) return "Already claimed today!";

            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string yesterday = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");

            if (LastDailyDate == yesterday)
            {
                DailyStreak++;
            }
            else
            {
                DailyStreak = 1;
            }

            LastDailyDate = today;
            int dayInCycle = ((DailyStreak - 1) % 7) + 1;
            string rewardDesc = "";

            switch (dayInCycle)
            {
                case 1: AddCoins(100); rewardDesc = "100 Gold Coins!"; break;
                case 2: AddCoins(200); rewardDesc = "200 Gold Coins!"; break;
                case 3: BonusShield = true; rewardDesc = "Free Shield Start!"; break;
                case 4: AddCoins(350); rewardDesc = "350 Gold Coins!"; break;
                case 5: BonusMagnet = true; rewardDesc = "Free Magnet Start!"; break;
                case 6: AddCoins(600); rewardDesc = "600 Gold Coins!"; break;
                case 7:
                    AddCoins(1000);
                    BonusFly = true;
                    rewardDesc = "1000 Coins + Flight Start!";
                    break;
            }

            Save();
            return rewardDesc;
        }

        // Missions
        void InitMissions()
        {
            Missions.Clear();
            int mCount = PlayerPrefs.GetInt("jd_mission_count", 0);
            if (mCount <= 0)
            {
                Missions.Add(new Mission { type = 0, title = "Collector: Grab 50 Coins", target = 50, reward = 100 });
                Missions.Add(new Mission { type = 1, title = "Agile: Slide 8 Times", target = 8, reward = 80 });
                Missions.Add(new Mission { type = 2, title = "Marathon: Dash 400m", target = 400, reward = 150 });
                SaveMissions();
            }
            else
            {
                for (int i = 0; i < mCount; i++)
                {
                    Missions.Add(new Mission
                    {
                        type = PlayerPrefs.GetInt("jd_m_type_" + i, 0),
                        title = PlayerPrefs.GetString("jd_m_title_" + i, "Mission"),
                        target = PlayerPrefs.GetInt("jd_m_target_" + i, 10),
                        current = PlayerPrefs.GetInt("jd_m_cur_" + i, 0),
                        reward = PlayerPrefs.GetInt("jd_m_rew_" + i, 50),
                        completed = PlayerPrefs.GetInt("jd_m_comp_" + i, 0) == 1,
                        claimed = PlayerPrefs.GetInt("jd_m_claim_" + i, 0) == 1
                    });
                }
            }
        }

        public void SaveMissions()
        {
            PlayerPrefs.SetInt("jd_mission_count", Missions.Count);
            for (int i = 0; i < Missions.Count; i++)
            {
                PlayerPrefs.SetInt("jd_m_type_" + i, Missions[i].type);
                PlayerPrefs.SetString("jd_m_title_" + i, Missions[i].title);
                PlayerPrefs.SetInt("jd_m_target_" + i, Missions[i].target);
                PlayerPrefs.SetInt("jd_m_cur_" + i, Missions[i].current);
                PlayerPrefs.SetInt("jd_m_rew_" + i, Missions[i].reward);
                PlayerPrefs.SetInt("jd_m_comp_" + i, Missions[i].completed ? 1 : 0);
                PlayerPrefs.SetInt("jd_m_claim_" + i, Missions[i].claimed ? 1 : 0);
            }
        }

        public void ReportMissionProgress(int missionType, int amount, bool isAbsolute = false)
        {
            bool anyChanged = false;
            foreach (var m in Missions)
            {
                if (m.type == missionType && !m.completed)
                {
                    if (isAbsolute)
                    {
                        if (amount > m.current) m.current = amount;
                    }
                    else
                    {
                        m.current += amount;
                    }

                    if (m.current >= m.target)
                    {
                        m.current = m.target;
                        m.completed = true;
                    }
                    anyChanged = true;
                }
            }
            if (anyChanged) SaveMissions();
        }

        public bool ClaimMission(int idx)
        {
            if (idx < 0 || idx >= Missions.Count) return false;
            Mission m = Missions[idx];
            if (m.completed && !m.claimed)
            {
                m.claimed = true;
                AddCoins(m.reward);

                // Generate new mission
                ReplaceMission(idx);
                SaveMissions();
                return true;
            }
            return false;
        }

        void ReplaceMission(int idx)
        {
            int rType = UnityEngine.Random.Range(0, 3);
            Mission newM = new Mission { type = rType };
            if (rType == 0)
            {
                newM.target = UnityEngine.Random.Range(40, 100);
                newM.title = "Collector: Grab " + newM.target + " Coins";
                newM.reward = newM.target * 2;
            }
            else if (rType == 1)
            {
                newM.target = UnityEngine.Random.Range(6, 15);
                newM.title = "Agile: Slide " + newM.target + " Times";
                newM.reward = newM.target * 12;
            }
            else
            {
                newM.target = UnityEngine.Random.Range(350, 900);
                newM.title = "Marathon: Dash " + newM.target + "m";
                newM.reward = Mathf.RoundToInt(newM.target * 0.35f);
            }
            Missions[idx] = newM;
        }
    }
}
