namespace RHToolkit.Models.PCK
{
    public class PCKFileNode(string name, PCKFile file)
    {
        public string Name { get; private set; } = name;
        public PCKFile PCKFile { get; set; } = file;
        public bool IsDir { get { return PCKFile == null; } }
        public SortedDictionary<string, PCKFileNode>? Nodes { get; set; }

        private bool isChecked;
        public bool IsChecked
        {
            get { return isChecked; }
            set
            {
                isChecked = value;
                if (PCKFile != null) PCKFile.IsChecked = isChecked;
            }
        }
    }
}
