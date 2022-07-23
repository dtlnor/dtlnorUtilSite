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
        public string CharmCount { get; set; }

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

            foreach (var skillPair in combins)
            {
                var skill1 = skillMaxsDict[skillPair.First()];
                var skill2 = skillMaxsDict[skillPair.Last()];
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

        public struct CharmRecord
        {
            public int skill1ID;
            public int skill2ID;
            public int skill1Lv;
            public int skill2Lv;
        }

        protected void ShowLimitedCharmCombins(int MaxWeight = 5, int Lv4SlotWeight = 2)
        {
            OutPutValue = string.Join(",", SelectedIds.Select(x => nameLookUp[int.Parse(x)]).ToList());

            IEnumerable<IEnumerable<int>> combins = GetPermutations(SelectedIds.Select(int.Parse).ToList(), 2);
            var skillMaxsDict = skillMaxs.ToDictionary(s => s.SkillID, s => s);
            var slotMaxsDict = slotMaxs.ToDictionary(s => s.Grade, s => s);
            var decoDict = decoBaseDatas
                .Where(d => d.SkillLv == 1)
                .ToDictionary(d => d.SkillID, d => d);
            // construct strings
            string result = string.Empty;

            var charmList = new List<List<KeyValuePair<int,int>>>();
            foreach (var skillPair in combins)
            {
                var skill1 = skillMaxsDict[skillPair.First()];
                var skill2 = skillMaxsDict[skillPair.Last()];
                var slotMaxCombine = slotMaxsDict[skill1.Grade + skill2.Grade];
                var skill1Deco = decoDict[skill1.SkillID];
                var skill2Deco = decoDict[skill2.SkillID];
                int skill1MaxWeight = (skill1Deco.DecoLv >= 2) ? skill1.Skill1Max : 0;
                int skill2MaxWeight = (skill2Deco.DecoLv >= 2) ? skill2.Skill2Max : 0;
                int slotWeight = (slotMaxCombine.Slot1Lv >= 2 ? 1 : 0) + (slotMaxCombine.Slot2Lv >= 2 ? 1 : 0) + (slotMaxCombine.Slot3Lv >= 2 ? 1 : 0);

                CharmRecord baseCharm; //Max
                baseCharm.skill1ID = skill1.SkillID;
                baseCharm.skill2ID = skill2.SkillID;
                baseCharm.skill1Lv = skill1.Skill1Max;
                baseCharm.skill2Lv = skill1.Skill2Max;

                // for Lv3 Slot
                int Lv3SkillTotalWeight = MaxWeight - slotWeight;

                if (Lv3SkillTotalWeight >= skill1MaxWeight + skill2MaxWeight)
                {
                    var shit = new List<KeyValuePair<int, int>>() {
                        new KeyValuePair<int, int>(skillPair.First(), skill1.Skill1Max),
                        new KeyValuePair<int, int>(skillPair.Last(), skill2.Skill2Max)
                    };
                }
                else
                {
                    var shitlist = GetLimitWeightCombination(baseCharm, skill1MaxWeight, skill2MaxWeight, Lv3SkillTotalWeight);
                }



                // for Lv4 Slot
                int Lv4SkillTotalWeight = MaxWeight - Lv4SlotWeight;

                
            }
            //CharmCombinText = result;
            StateHasChanged();
        }
        protected static List<CharmRecord> GetLimitWeightCombination(CharmRecord baseCharm, int skill1Max, int Skill2Max, int SkillTotalMax)
        {
            var skillLvPair = new List<CharmRecord>();

            // for skill 1 and 2 both weighted
            foreach (int i in Enumerable.Range(1, SkillTotalMax))
            {
                int skill1Lv = i;
                int skill2Lv = SkillTotalMax - i;

                if ((skill1Lv <= skill1Max) && (skill2Lv <= Skill2Max))
                {
                    CharmRecord newCharm = baseCharm;
                    newCharm.skill1Lv = skill1Lv;
                    newCharm.skill2Lv = skill2Lv;
                    skillLvPair.Add(baseCharm);
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
                { LangIndex.ara, "العربية" },
                { LangIndex.chS, "简体中文" },
                { LangIndex.chT, "繁體中文" },
                { LangIndex.eng, "English" },
                { LangIndex.fre, "Français" },
                { LangIndex.ger, "Deutsch" },
                { LangIndex.ita, "Italiano" },
                { LangIndex.jpn, "日本語" },
                { LangIndex.kor, "한국어" },
                { LangIndex.pol, "Polski" },
                { LangIndex.ptB, "Português do Brasil" },
                { LangIndex.rus, "Русский" },
                { LangIndex.spa, "Español" }
            };
        }
    }
}