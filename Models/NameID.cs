﻿namespace RHGMTool.Models
{
    public class NameID
    {
        public int ID { get; set; }
        public string? Name { get; set; }

        public override string? ToString()
        {
            return Name;
        }

    }
}
