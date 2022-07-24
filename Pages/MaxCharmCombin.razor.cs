using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http.Json;
using dtlnorUtilSite;
using dtlnorUtilSite.Shared;
using System;
using System.Linq;

namespace dtlnorUtilSite.Pages
{
    public partial class MaxCharmCombin
    {
        private JObject skillNameIDPair;
        private SkillMax[] skillMaxs;
        private SlotMax[] slotMaxs;
        private DecoBaseData[] decoBaseDatas;
        private SkillBaseData[] skillBaseDatas;
        private Dictionary<int, string> nameLookUp;

        protected List<string> SelectedIds = new List<string>();

        public string OutPutValue { get; set; }
        public List<string> CharmCombinText { get; set; } = new List<string>();
        public string CharmCount { get; set; } = string.Empty;
        public Global.LangIndex SkillNameLang { get; set; } = Global.LangIndex.chS;
        public int MaxWeight { get; set; } = 5;
        public int Lv4SlotWeight { get; set; } = 2;

        static readonly List<int> newMeldSkill = new()
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
            131
        };

        static readonly List<int> noSkillOneInNewMeld = new()
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
            98
        };

        static readonly List<int> noSkillTwoInNewMeld = new()
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
            98
        };

        protected void ShowSelectedValues()
        {
            OutPutValue = string.Join(", ", SelectedIds.ToArray());
            StateHasChanged();
        }
        static IEnumerable<IEnumerable<T>> GetPermutations<T>(IEnumerable<T> list, int length)
        {
            if (length == 1) return list.Select(t => new T[] { t });

            return GetPermutations(list, length - 1)
                .SelectMany(t => list.Where(e => !t.Contains(e)),
                    (t1, t2) => t1.Concat(new T[] { t2 }));
        }

        static IEnumerable<IEnumerable<T>> PermutationsWithRepetition<T>(IEnumerable<T> list, int length)
        {
            if (length <= 0)
                yield return Array.Empty<T>();
            else
            {
                foreach (var i in list)
                    foreach (var c in PermutationsWithRepetition(list, length - 1))
                        yield return c.Concat(new T[] { i });
            }
        }

        protected void ShowCharmCombins()
        {
            OutPutValue = string.Join(",", SelectedIds.Select(x => nameLookUp[int.Parse(x)]).ToList());

            var skillMaxsDict = skillMaxs.ToDictionary(s => s.SkillID, s => s);
            var slotMaxsDict = slotMaxs.ToDictionary(s => s.Grade, s => s);

            IEnumerable<IEnumerable<int>> combins = GetPermutations(SelectedIds.Select(int.Parse).ToList(), 2);
            // construct strings
            int charmCount = 0;

            var sb = new System.Text.StringBuilder();
            var stringChunk = new List<string>();

            var charmList = new HashSet<UInt32>();
            foreach (var skillPair in combins)
            {
                var skill1 = skillMaxsDict[skillPair.First()];
                var skill2 = skillMaxsDict[skillPair.Last()];

                if (newMeldSkill.Contains(skillPair.Last()) && noSkillOneInNewMeld.Contains(skillPair.First()))
                {
                    continue;
                }
                if (newMeldSkill.Contains(skillPair.First()) && noSkillTwoInNewMeld.Contains(skillPair.Last()))
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

                UInt32 baseCharm = ConstructCharmRecord(skill1.SkillID, skill2.SkillID, skill1.Skill1Max, skill2.Skill2Max);

                UInt32 reverseCharm = ConstructCharmRecord(skill2.SkillID, skill1.SkillID, skill2.Skill2Max, skill1.Skill1Max);

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
                    skill2.Skill2Max.ToString()});

                sb.AppendLine(string.Join(",", new List<string>
                {
                    skillPart,
                    slotMaxCombine.Slot1Lv.ToString(),
                    slotMaxCombine.Slot2Lv.ToString(),
                    slotMaxCombine.Slot3Lv.ToString()
                }));
                charmCount++;
                if (charmCount % 2500 == 0 && charmCount > 0)
                {
                    stringChunk.Add(sb.ToString());
                    sb.Clear();
                }

                sb.AppendLine(skillPart + ",4,0,0");
                charmCount++;
                if (charmCount % 2500 == 0 && charmCount > 0)
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

        ////public record CharmRecord
        ////{
        ////    public int skill1ID;
        ////    public int skill2ID;
        ////    public int skill1Lv;
        ////    public int skill2Lv;
        ////}

        public static UInt32 ConstructCharmRecord(int skill1ID, int skill2ID, int skill1Lv, int skill2Lv)
        {
            return (UInt32)((skill1ID << 24) + (skill2ID << 16) + (skill1Lv << 8) + skill2Lv);
        }

        public static (int, int, int, int) DestructCharmRecord(UInt32 charm)
        {
            return
            (
                (int)(charm >> 24),
                (int)((charm >> 16) & 0xFF),
                (int)((charm >> 8) & 0xFF),
                (int)(charm & 0xFF)
            );
        }

        public static UInt32 GetReverseCharmRecord(UInt32 charm)
        {
            return (UInt32)(
                ((charm & 0xFF000000) >> 8) +
                ((charm & 0x00FF0000) << 8) +
                ((charm & 0x0000FF00) >> 8) +
                ((charm & 0x000000FF) << 8)
                );
        }

        protected void ShowLimitedCharmCombins()
        {
            OutPutValue = string.Join(",", SelectedIds.Select(x => nameLookUp[int.Parse(x)]).ToList());

            var skillMaxsDict = skillMaxs.ToDictionary(s => s.SkillID, s => s);
            var slotMaxsDict = slotMaxs.ToDictionary(s => s.Grade, s => s);
            var decoDict = decoBaseDatas
                .Where(d => d.SkillLv == 1)
                .ToDictionary(d => d.SkillID, d => d);

            IEnumerable<IEnumerable<int>> combins = GetPermutations(SelectedIds.Select(int.Parse).ToList(), 2);
            // construct strings
            int charmCount = 0;

            var sb = new System.Text.StringBuilder();
            var stringChunk = new List<string>();

            var lv3CharmList = new HashSet<UInt32>();
            var lv4CharmList = new HashSet<UInt32>();
            foreach (var skillPair in combins)
            {
                var skill1 = skillMaxsDict[skillPair.First()];
                var skill2 = skillMaxsDict[skillPair.Last()];

                if (newMeldSkill.Contains(skillPair.Last()) && noSkillOneInNewMeld.Contains(skillPair.First()))
                {
                    continue;
                }
                if (newMeldSkill.Contains(skillPair.First()) && noSkillTwoInNewMeld.Contains(skillPair.Last()))
                {
                    continue;
                }

                UInt32 baseCharm = ConstructCharmRecord(skill1.SkillID, skill2.SkillID, skill1.Skill1Max, skill2.Skill2Max);
                UInt32 reverseCharm = ConstructCharmRecord(skill2.SkillID, skill1.SkillID, skill2.Skill2Max, skill1.Skill1Max);

                ////if (charmList.Contains(baseCharm) || charmList.Contains(reverseCharm))
                ////{
                ////    continue;
                ////}

                // construct strings

                var slotMaxCombine = slotMaxsDict[skill1.Grade + skill2.Grade];
                var hasDeco = decoDict.TryGetValue(skill1.SkillID, out DecoBaseData skill1Deco);
                if (!hasDeco){
                    skill1Deco = new DecoBaseData()
                    {
                        BasePrice = 0,
                        DecoID = 0,
                        DecoLv = 2,
                        IconColor = 0,
                        Rare = 0,
                        SkillID = skill1.SkillID,
                        SkillLv = 1,
                        SortID = 0
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
                        SortID = 0
                    };
                }

                int skill1MaxWeight = (skill1Deco.DecoLv >= 2) ? skill1.Skill1Max : 0;
                int skill2MaxWeight = (skill2Deco.DecoLv >= 2) ? skill2.Skill2Max : 0;
                int slotWeight = (slotMaxCombine.Slot1Lv >= 2 ? 1 : 0) + (slotMaxCombine.Slot2Lv >= 2 ? 1 : 0) + (slotMaxCombine.Slot3Lv >= 2 ? 1 : 0);

                void printString(UInt32 charm, bool Lv3)
                {
                    (int subSkill1ID, int subSkill2ID, int subSkill1Lv, int subSkill2Lv) = DestructCharmRecord(charm);

                    string skillPart = string.Join(",", new List<string>
                        {
                            nameLookUp[subSkill1ID],
                            subSkill1Lv.ToString(),
                            nameLookUp[subSkill2ID],
                            subSkill2Lv.ToString()
                        });

                    if (Lv3)
                    {
                        sb.AppendLine(string.Join(",", new List<string>
                            {
                                skillPart,
                                slotMaxCombine.Slot1Lv.ToString(),
                                slotMaxCombine.Slot2Lv.ToString(),
                                slotMaxCombine.Slot3Lv.ToString()
                            }));
                    }
                    else
                    {
                        sb.AppendLine(skillPart + ",4,0,0");
                    }

                    charmCount++;
                    if (charmCount % 2500 == 0 && charmCount > 0)
                    {
                        stringChunk.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                // TODO : no empty sencond skill pls
                // for Lv3 Slot
                int Lv3SkillTotalWeight = MaxWeight - slotWeight;

                if (Lv3SkillTotalWeight >= skill1MaxWeight + skill2MaxWeight)
                {
                    if (lv3CharmList.Contains(baseCharm) || lv3CharmList.Contains(reverseCharm))
                    {
                        continue;
                    }
                    lv3CharmList.Add(baseCharm);
                    printString(baseCharm, true);
                }
                else
                {
                    var subCharmList = GetLimitWeightCombination(baseCharm, skill1MaxWeight, skill2MaxWeight, Lv3SkillTotalWeight);
                    foreach (var subCharm in subCharmList)
                    {
                        if (lv3CharmList.Contains(subCharm) || lv3CharmList.Contains(GetReverseCharmRecord(subCharm)))
                        {
                            continue;
                        }
                        lv3CharmList.Add(subCharm);
                        printString(subCharm, true);
                    }
                }

                // for Lv4 Slot
                int Lv4SkillTotalWeight = MaxWeight - Lv4SlotWeight;
                if (Lv4SkillTotalWeight >= skill1MaxWeight + skill2MaxWeight)
                {

                    if (lv4CharmList.Contains(baseCharm) || lv4CharmList.Contains(reverseCharm))
                    {
                        continue;
                    }
                    lv4CharmList.Add(baseCharm);
                    printString(baseCharm, false);
                }
                else
                {
                    var subCharmList = GetLimitWeightCombination(baseCharm, skill1MaxWeight, skill2MaxWeight, Lv4SkillTotalWeight);
                    foreach (var subCharm in subCharmList)
                    {
                        if (lv4CharmList.Contains(subCharm) || lv4CharmList.Contains(GetReverseCharmRecord(subCharm)))
                        {
                            continue;
                        }
                        lv4CharmList.Add(subCharm);
                        printString(subCharm, false);
                    }
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

        protected static List<UInt32> GetLimitWeightCombination(UInt32 baseCharm, int skill1MaxWeight, int skill2MaxWeight, int SkillTotalMax)
        {
            var skillLvPair = new List<UInt32>();

            // for skill 1 and 2 both weighted
            // bug when limited but not weighted

            int skill1Lv;
            int skill2Lv;
            foreach (int i in Enumerable.Range(1, SkillTotalMax - 1))
            {
                skill1Lv = i;
                skill2Lv = SkillTotalMax - i;

                if ((skill1Lv <= skill1MaxWeight) && (skill2Lv <= skill2MaxWeight))
                {
                    UInt32 newCharm = (UInt32)((baseCharm & 0xFFFF0000) + (skill1Lv << 8) + skill2Lv);
                    skillLvPair.Add(newCharm);
                }
            }
            skill1Lv = SkillTotalMax;
            skill2Lv = 0;
            if ((skill1Lv <= skill1MaxWeight) && (skill2Lv <= skill2MaxWeight))
            {
                if (skill2MaxWeight == 0)
                {
                    UInt32 newCharm = (UInt32)((baseCharm & 0xFFFF00FF) + (skill1Lv << 8));
                    skillLvPair.Add(newCharm);
                }
                else
                {
                    UInt32 newCharm = (UInt32)((baseCharm & 0xFF000000) + (skill1Lv << 8));
                    skillLvPair.Add(newCharm);
                }
            }

            return skillLvPair;
        }

        protected override async Task OnInitializedAsync()
        {
            skillNameIDPair = JObject.Parse((await Http.GetFromJsonAsync<System.Text.Json.JsonElement>("skill-data/SKILL_NAME_LOOKUP.json")).GetRawText());
            skillMaxs = await Http.GetFromJsonAsync<SkillMax[]>("skill-data/SkillMax.json");
            slotMaxs = await Http.GetFromJsonAsync<SlotMax[]>("skill-data/SlotMax.json");
            decoBaseDatas = await Http.GetFromJsonAsync<DecoBaseData[]>("skill-data/DecoBaseData.json");
            skillBaseDatas = await Http.GetFromJsonAsync<SkillBaseData[]>("skill-data/SkillBaseData.json");
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
            public enum LangIndex
            {
                jpn,
                eng,
                fre,
                ita,
                ger,
                spa,
                rus,
                ptB = 10,
                kor,
                chT,
                chS,
                ara = 21,
                pol = 26
            }

            public static readonly List<LangIndex> LANGUAGES = Enum.GetValues(typeof(LangIndex)).Cast<LangIndex>().ToList();

            public static readonly Dictionary<LangIndex, string> LANGUAGE_NAME_LOOKUP = new()
            {
                //{ LangIndex.ara, "العربية" },
                { LangIndex.chS, "简体中文" },
                { LangIndex.chT, "繁體中文" },
                { LangIndex.eng, "English" },
                //{ LangIndex.fre, "Français" },
                //{ LangIndex.ger, "Deutsch" },
                //{ LangIndex.ita, "Italiano" },
                { LangIndex.jpn, "日本語" },
                { LangIndex.kor, "한국어" },
                //{ LangIndex.pol, "Polski" },
                //{ LangIndex.ptB, "Português do Brasil" },
                //{ LangIndex.rus, "Русский" },
                //{ LangIndex.spa, "Español" }
            };
        }
    }
}