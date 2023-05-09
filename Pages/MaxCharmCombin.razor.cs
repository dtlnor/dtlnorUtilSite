namespace dtlnorUtilSite.Pages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http.Json;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using Toolbelt.Blazor.I18nText;

    public partial class MaxCharmCombin
    {
        public static readonly int MaxCharmInArmorSearch = 2500;

        public static readonly List<int> NewMeldSkill = new()
        {
            116,
            122,
            123,
            124,
            125,
            126,
            127,
            128,
            129,
            131,
        };

        public static readonly List<int> NoSkillOneInNewMeld = new()
        {
            13,
            14,
            15,
            16,
            17,
            18,
            28,
            67,
            68,
            69,
            70,
            71,
            72,
            73,
            74,
            75,
            77,
            78,
            79,
            80,
            86,
            94,
            97,
            98,
        };

        public static readonly List<int> NoSkillTwoInNewMeld = new()
        {
            28,
            67,
            68,
            69,
            70,
            71,
            73,
            74,
            75,
            77,
            78,
            79,
            80,
            86,
            94,
            97,
            98,
        };

        protected HashSet<string> selectedIds = new();

        private JObject skillNameIDPair;
        private SkillMax[] skillMaxs;
        private SlotMax[] slotMaxs;
        private DecoBaseData[] decoBaseDatas;
        ////private SkillBaseData[] skillBaseDatas;
        private Dictionary<int, string> nameLookUp;

        protected string OutPutValue { get; set; }

        protected List<string> CharmCombinText { get; set; } = new List<string>();

        protected string CharmCount { get; set; } = string.Empty;

        [Obsolete("Now control by i18n")]
        protected Global.LangIndex SkillNameLang { get; set; } = Global.LangIndex.English;

        protected int MaxWeight { get; set; } = 5;

        protected int Lv4SlotWeight { get; set; } = 2;

        protected static IEnumerable<IEnumerable<T>> GetPermutations<T>(IEnumerable<T> list, int length)
        {
            if (length == 1)
            {
                return list.Select(t => new T[] { t });
            }

            return GetPermutations(list, length - 1)
                .SelectMany(
                    t => list.Where(e => !t.Contains(e)),
                    (t1, t2) => t1.Concat(new T[] { t2 }));
        }

        protected static IEnumerable<IEnumerable<T>> PermutationsWithRepetition<T>(IEnumerable<T> list, int length)
        {
            if (length <= 0)
            {
                yield return Array.Empty<T>();
            }
            else
            {
                foreach (var i in list)
                {
                    foreach (var c in PermutationsWithRepetition(list, length - 1))
                    {
                        yield return c.Concat(new T[] { i });
                    }
                }
            }
        }

        protected static uint ConstructCharmRecord(int skill1ID, int skill2ID, int skill1Lv, int skill2Lv) =>
            (uint)((skill1ID << 24) + (skill2ID << 16) + (skill1Lv << 8) + skill2Lv);

        protected static (int Skill1ID, int Skill2ID, int Skill1Lv, int Skill2Lv) DestructCharmRecord(uint charm) => (
                (int)(charm >> 24),
                (int)((charm >> 16) & 0xFF),
                (int)((charm >> 8) & 0xFF),
                (int)(charm & 0xFF));

        protected static uint GetReverseCharmRecord(uint charm) =>
                ((charm & 0xFF000000) >> 8) +
                ((charm & 0x00FF0000) << 8) +
                ((charm & 0x0000FF00) >> 8) +
                ((charm & 0x000000FF) << 8);

        protected static List<uint> GetLimitWeightCombination(uint baseCharm, int skill1MaxWeight, int skill2MaxWeight, int skillTotalMax)
        {
            var skillLvPair = new List<uint>();

            int skill1Lv;
            int skill2Lv;
            foreach (int i in Enumerable.Range(1, skillTotalMax - 1))
            {
                skill1Lv = i;
                skill2Lv = skillTotalMax - i;

                // there is logical bug here as it bug when skill isn't weighted,
                // but as we are finding at least 3 effective skill charm,
                // and always give the weight to slot first,
                // and no weighted skill more than 3 available skill Lv,
                // it is save to get correct result by skill2Lv = 0.
                if ((skill1Lv <= skill1MaxWeight) && (skill2Lv <= skill2MaxWeight))
                {
                    uint newCharm = (uint)((baseCharm & 0xFFFF0000) + (skill1Lv << 8) + skill2Lv);
                    skillLvPair.Add(newCharm);
                }
            }

            skill1Lv = skillTotalMax;
            skill2Lv = 0;
            if ((skill1Lv <= skill1MaxWeight) && (skill2Lv <= skill2MaxWeight))
            {
                if (skill2MaxWeight == 0)
                {
                    uint newCharm = (uint)((baseCharm & 0xFFFF00FF) + (skill1Lv << 8));
                    skillLvPair.Add(newCharm);
                }
                else
                {
                    uint newCharm = (uint)((baseCharm & 0xFF000000) + (skill1Lv << 8));
                    skillLvPair.Add(newCharm);
                }
            }

            return skillLvPair;
        }

        [Obsolete("For Max Charm only, use OnShowCharmCombinations() instead.")]
        protected void ShowCharmCombins()
        {
            OutPutValue = string.Join(", ", selectedIds.Select(x => nameLookUp[int.Parse(x)]).ToList());

            var skillMaxsDict = skillMaxs.ToDictionary(s => s.SkillID, s => s);
            var slotMaxsDict = slotMaxs.ToDictionary(s => s.Grade, s => s);

            IEnumerable<IEnumerable<int>> combins = GetPermutations(selectedIds.Select(int.Parse).ToList(), 2);

            // construct strings
            int charmCount = 0;

            var sb = new System.Text.StringBuilder();
            var stringChunk = new List<string>();

            var charmList = new HashSet<uint>();
            foreach (var skillPair in combins)
            {
                var skill1 = skillMaxsDict[skillPair.First()];
                var skill2 = skillMaxsDict[skillPair.Last()];

                if (NewMeldSkill.Contains(skillPair.Last()) && NoSkillOneInNewMeld.Contains(skillPair.First()))
                {
                    continue;
                }

                if (NewMeldSkill.Contains(skillPair.First()) && NoSkillTwoInNewMeld.Contains(skillPair.Last()))
                {
                    continue;
                }

                ////CharmRecord baseCharm = new()
                ////{
                ////    skill1ID = skill1.SkillID,
                ////    skill2ID = skill2.SkillID,
                ////    skill1Lv = skill1.Skill1Max,
                ////    skill2Lv = skill1.Skill2Max
                ////};

                ////CharmRecord reverseCharm = new()
                ////{
                ////    skill1ID = baseCharm.skill2ID,
                ////    skill2ID = baseCharm.skill1ID,
                ////    skill1Lv = baseCharm.skill2Lv,
                ////    skill2Lv = baseCharm.skill1Lv
                ////};

                uint baseCharm = ConstructCharmRecord(skill1.SkillID, skill2.SkillID, skill1.Skill1Max, skill2.Skill2Max);

                uint reverseCharm = ConstructCharmRecord(skill2.SkillID, skill1.SkillID, skill2.Skill2Max, skill1.Skill1Max);

                if (charmList.Contains(baseCharm) || charmList.Contains(reverseCharm))
                {
                    continue;
                }

                charmList.Add(baseCharm);

                // construct strings
                var slotMaxCombine = slotMaxsDict[skill1.Grade + skill2.Grade];
                string skillPart = string.Join(",", new List<string>
                {
                    nameLookUp[skill1.SkillID],
                    skill1.Skill1Max.ToString(),
                    nameLookUp[skill2.SkillID],
                    skill2.Skill2Max.ToString(),
                });

                sb.AppendLine(string.Join(",", new List<string>
                {
                    skillPart,
                    slotMaxCombine.Slot1Lv.ToString(),
                    slotMaxCombine.Slot2Lv.ToString(),
                    slotMaxCombine.Slot3Lv.ToString(),
                }));
                charmCount++;
                if (charmCount % MaxCharmInArmorSearch == 0 && charmCount > 0)
                {
                    stringChunk.Add(sb.ToString());
                    sb.Clear();
                }

                sb.AppendLine(skillPart + ",4,0,0");
                charmCount++;
                if (charmCount % MaxCharmInArmorSearch == 0 && charmCount > 0)
                {
                    stringChunk.Add(sb.ToString());
                    sb.Clear();
                }
            }

            if (sb.Length > 0)
            {
                stringChunk.Add(sb.ToString());
                sb.Clear();
            }

            CharmCount = charmCount.ToString();
            CharmCombinText = stringChunk;
            StateHasChanged();
        }

        protected void OnShowCharmCombinations()
        {
            OutPutValue = string.Join(", ", selectedIds.Select(x => nameLookUp[int.Parse(x)]).ToList());
            StateHasChanged();

            var skillMaxsDict = skillMaxs.ToDictionary(s => s.SkillID, s => s);
            var slotMaxsDict = slotMaxs.ToDictionary(s => s.Grade, s => s);
            var decoDict = decoBaseDatas
                .Where(d => d.SkillLv == 1)
                .ToDictionary(d => d.SkillID, d => d);

            IEnumerable<IEnumerable<int>> combins = GetPermutations(selectedIds.Select(int.Parse).ToList(), 2);

            // construct strings
            int charmCount = 0;

            var sb = new System.Text.StringBuilder();
            var stringChunk = new List<string>();

            var lv3CharmList = new HashSet<uint>();
            var lv4CharmList = new HashSet<uint>();
            foreach (var skillPair in combins)
            {
                var skill1 = skillMaxsDict[skillPair.First()];
                var skill2 = skillMaxsDict[skillPair.Last()];

                if (NewMeldSkill.Contains(skillPair.Last()) && NoSkillOneInNewMeld.Contains(skillPair.First()))
                {
                    continue;
                }

                if (NewMeldSkill.Contains(skillPair.First()) && NoSkillTwoInNewMeld.Contains(skillPair.Last()))
                {
                    continue;
                }

                uint baseCharm = ConstructCharmRecord(skill1.SkillID, skill2.SkillID, skill1.Skill1Max, skill2.Skill2Max);
                uint reverseCharm = ConstructCharmRecord(skill2.SkillID, skill1.SkillID, skill2.Skill2Max, skill1.Skill1Max);

                ////if (charmList.Contains(baseCharm) || charmList.Contains(reverseCharm))
                ////{
                ////    continue;
                ////}

                var slotMaxCombine = slotMaxsDict[skill1.Grade + skill2.Grade];
                var hasDeco = decoDict.TryGetValue(skill1.SkillID, out DecoBaseData skill1Deco);
                if (!hasDeco)
                {
                    skill1Deco = new DecoBaseData()
                    {
                        BasePrice = 0,
                        DecoID = 0,
                        DecoLv = 2,
                        IconColor = 0,
                        Rare = 0,
                        SkillID = skill1.SkillID,
                        SkillLv = 1,
                        SortID = 0,
                    };
                }

                hasDeco = decoDict.TryGetValue(skill2.SkillID, out DecoBaseData skill2Deco);
                if (!hasDeco)
                {
                    skill2Deco = new DecoBaseData()
                    {
                        BasePrice = 0,
                        DecoID = 0,
                        DecoLv = 2,
                        IconColor = 0,
                        Rare = 0,
                        SkillID = skill2.SkillID,
                        SkillLv = 1,
                        SortID = 0,
                    };
                }

                int skill1MaxWeight = (skill1Deco.DecoLv >= 2) ? skill1.Skill1Max : 0;
                int skill2MaxWeight = (skill2Deco.DecoLv >= 2) ? skill2.Skill2Max : 0;
                int slotWeight = (slotMaxCombine.Slot1Lv >= 2 ? 1 : 0) + (slotMaxCombine.Slot2Lv >= 2 ? 1 : 0) + (slotMaxCombine.Slot3Lv >= 2 ? 1 : 0);

                void PrintString(uint charm, bool lv3)
                {
                    (int subSkill1ID, int subSkill2ID, int subSkill1Lv, int subSkill2Lv) = DestructCharmRecord(charm);

                    string skillPart = string.Join(",", new List<string>
                        {
                            nameLookUp[subSkill1ID],
                            subSkill1Lv.ToString(),
                            nameLookUp[subSkill2ID],
                            subSkill2Lv.ToString(),
                        });

                    if (lv3)
                    {
                        sb.AppendLine(string.Join(",", new List<string>
                            {
                                skillPart,
                                slotMaxCombine.Slot1Lv.ToString(),
                                slotMaxCombine.Slot2Lv.ToString(),
                                slotMaxCombine.Slot3Lv.ToString(),
                            }));
                    }
                    else
                    {
                        sb.AppendLine(skillPart + ",4,0,0");
                    }

                    charmCount++;
                    if (charmCount % MaxCharmInArmorSearch == 0 && charmCount > 0)
                    {
                        stringChunk.Add(sb.ToString());
                        sb.Clear();
                    }
                }

                void DoSubList(int skillTotalWeight, bool isLv3, ref HashSet<uint> charmList)
                {
                    if (skillTotalWeight >= skill1MaxWeight + skill2MaxWeight)
                    {
                        if (charmList.Contains(baseCharm) || charmList.Contains(reverseCharm))
                        {
                            return;
                        }

                        charmList.Add(baseCharm);
                        PrintString(baseCharm, isLv3);
                    }
                    else
                    {
                        var subCharmList = GetLimitWeightCombination(baseCharm, skill1MaxWeight, skill2MaxWeight, skillTotalWeight);
                        foreach (var subCharm in subCharmList)
                        {
                            if (charmList.Contains(subCharm) || charmList.Contains(GetReverseCharmRecord(subCharm)))
                            {
                                continue;
                            }

                            charmList.Add(subCharm);
                            PrintString(subCharm, isLv3);
                        }
                    }
                }

                // for Lv3 Slot
                int lv3SkillTotalWeight = MaxWeight - slotWeight;
                DoSubList(lv3SkillTotalWeight, true, ref lv3CharmList);

                // for Lv4 Slot
                int lv4SkillTotalWeight = MaxWeight - Lv4SlotWeight;
                DoSubList(lv4SkillTotalWeight, false, ref lv4CharmList);
            }

            if (sb.Length > 0)
            {
                stringChunk.Add(sb.ToString());
                sb.Clear();
            }

            CharmCount = charmCount.ToString();
            CharmCombinText = stringChunk;
            StateHasChanged();
        }

        [Obsolete("use uint and construct method instead.")]
        public struct CharmRecord
        {
            public int Skill1ID;
            public int Skill2ID;
            public int Skill1Lv;
            public int Skill2Lv;
        }

        public class SkillMax
        {
            public int SkillID { get; set; }

            public string Grade { get; set; }

            public int Skill1Max { get; set; }

            public int Skill2Max { get; set; }
        }

        public class SkillBaseData
        {
            public int SkillID { get; set; }

            public int MaxLevel { get; set; }

            public int IconColor { get; set; }

            public int WorthPointList { get; set; }
        }

        public class SlotMax
        {
            public string Grade { get; set; }

            public int Slot1Lv { get; set; }

            public int Slot2Lv { get; set; }

            public int Slot3Lv { get; set; }
        }

        public class DecoBaseData
        {
            public int DecoID { get; set; }

            public int SortID { get; set; }

            public int Rare { get; set; }

            public int IconColor { get; set; }

            public int BasePrice { get; set; }

            public int DecoLv { get; set; }

            public int SkillID { get; set; }

            public int SkillLv { get; set; }
        }

        public class Global
        {
            public static readonly List<LangIndex> LANGUAGES = Enum.GetValues(typeof(LangIndex)).Cast<LangIndex>().ToList();

            public static readonly Dictionary<LangIndex, string> LANGUAGE_NAME_LOOKUP = new()
            {
                { LangIndex.SimplifiedChinese, "简体中文" },
                { LangIndex.TraditionalChinese, "繁體中文" },
                { LangIndex.English, "English" },
                { LangIndex.Japanese, "日本語" },
                { LangIndex.Korean, "한국어" }
            };

            public enum LangIndex
            {
                Japanese = 0,
                English = 1,
                French = 2,
                Italian = 3,
                German = 4,
                Spanish = 5,
                Russian = 6,
                Polish = 7,
                Dutch = 8,
                Portuguese = 9,
                PortugueseBr = 10,
                Korean = 11,
                TraditionalChinese = 12,
                SimplifiedChinese = 13,
                Finnish = 14,
                Swedish = 15,
                Danish = 16,
                Norwegian = 17,
                Czech = 18,
                Hungarian = 19,
                Slovak = 20,
                Arabic = 21,
                Turkish = 22,
                Bulgarian = 23,
                Greek = 24,
                Romanian = 25,
                Thai = 26,
                Ukrainian = 27,
                Vietnamese = 28,
                Indonesian = 29,
                Fiction = 30,
                Hindi = 31,
                LatinAmericanSpanish = 32,
                Max = 33,
                Unknown = 33,
            }
        }
    }
}