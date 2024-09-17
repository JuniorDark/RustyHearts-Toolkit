namespace RHToolkit.Models
{
    public class NameID
    {
        public int ID { get; set; }
        public string? Name { get; set; }
        public string? ImagePath { get; set; }
        public string? Type { get; set; }

        public override string? ToString()
        {
            return Name;
        }

    }
}
