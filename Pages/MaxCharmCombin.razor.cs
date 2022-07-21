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
        private SkillMax[] skillMax;
        private SlotMax[] slotMax;
        private DecoBaseData[] decoBaseData;
        private Dictionary<int, string> nameLookUp;

        protected List<string> SelectedIds = new List<string>();

        public string OutPutValue { get; set; }
        public string CharmCombinText { get; set; }

        protected void ShowSelectedValues()
        {
            OutPutValue = string.Join(",", SelectedIds.ToArray());
            StateHasChanged();
        }
        static IEnumerable<IEnumerable<T>> GetPermutations<T>(IEnumerable<T> list, int length)
        {
            if (length == 1) return list.Select(t => new T[] { t });

            return GetPermutations(list, length - 1)
                .SelectMany(t => list.Where(e => !t.Contains(e)),
                    (t1, t2) => t1.Concat(new T[] { t2 }));
        }

        protected void ShowCharmCombins()
        {
            OutPutValue = string.Join(",", SelectedIds.Select(x => nameLookUp[int.Parse(x)]).ToList());

            IEnumerable<IEnumerable<int>> combins = GetPermutations(SelectedIds.Select(int.Parse).ToList(), 2);
            // construct strings
            string result = string.Empty;
            foreach (var skillPair in combins)
            {
                var SkillMax1 = skillMax.Single(skill => skill.SkillID == skillPair.First());
                var SkillMax2 = skillMax.Single(skill => skill.SkillID == skillPair.Last());
                var SlotMaxCombine = slotMax.Single(gradeType => gradeType.Grade == SkillMax1.Grade + SkillMax2.Grade);
                string skillPart = string.Join(",", new List<string>
                {
                    nameLookUp[SkillMax1.SkillID],
                    SkillMax1.Skill1Max.ToString(),
                    nameLookUp[SkillMax2.SkillID],
                    SkillMax2.Skill1Max.ToString()});

                result += string.Join(",", new List<string>
                {
                    skillPart,
                    SlotMaxCombine.Slot1Lv.ToString(),
                    SlotMaxCombine.Slot2Lv.ToString(),
                    SlotMaxCombine.Slot3Lv.ToString() + "\n"
                });

                result += skillPart + ",4,0,0\n";
            }
            CharmCombinText = result;
            StateHasChanged();
        }

        protected override async Task OnInitializedAsync()
        {
            skillNameIDPair = JObject.Parse((await Http.GetFromJsonAsync<System.Text.Json.JsonElement>("skill-data/SKILL_NAME_LOOKUP.json")).GetRawText());
            skillMax = await Http.GetFromJsonAsync<SkillMax[]>("skill-data/SkillMax.json");
            slotMax = await Http.GetFromJsonAsync<SlotMax[]>("skill-data/SlotMax.json");
            decoBaseData = await Http.GetFromJsonAsync<DecoBaseData[]>("skill-data/DecoBaseData.json");
        }

        public class SkillMax
        {
            public int SkillID { get; set; }
            public string Grade { get; set; }
            public int Skill1Max { get; set; }
            public int Skill2Max { get; set; }
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