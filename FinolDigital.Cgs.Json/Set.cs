namespace FinolDigital.Cgs.Json
{
    using System;

    public class Set : IEquatable<Set>
    {
        public const string DefaultCode = "_CGSDEFAULT_";
        public const string DefaultName = "_CGSDEFAULT_";

        public string Code { get; }
        public string Name { get; set; }
        public string CardsUrl { get; set; }

        public Set(string code, string name, string? cardsUrl = null)
        {
            Code = string.IsNullOrEmpty(code) ? DefaultCode : code.Clone() as string ?? DefaultCode;
            Name = string.IsNullOrEmpty(name) ? DefaultName : name.Clone() as string ?? DefaultName;
            CardsUrl = string.IsNullOrEmpty(cardsUrl) ? string.Empty : cardsUrl.Clone() as string ?? string.Empty;
        }

        public virtual bool Equals(Set other)
        {
            return other != null && Code.Equals(other.Code);
        }

        public override string ToString()
        {
            return Code.Equals(Name) ? Code : string.Format("{1} ({0})", Code, Name);
        }
    }
}
