namespace OutWit.Examples.Services.ClientBlazor.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class OutNameAttribute : Attribute
    {
        public string Name { get; }

        public OutNameAttribute(string name)
        {
            Name = name;
        }
    }
}
