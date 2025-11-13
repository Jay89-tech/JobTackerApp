namespace SkillsManagement.Models
{
    public class GroupedSkillViewModel
    {
        public string SkillName { get; set; }
        public string Category { get; set; }
        public int Count { get; set; }
        public double AverageProficiency { get; set; }
        public List<string> Employees { get; set; }
    }

    public class SkillGapViewModel
    {
        public string SkillName { get; set; }
        public double AvgProficiency { get; set; }
        public int Count { get; set; }
    }
}