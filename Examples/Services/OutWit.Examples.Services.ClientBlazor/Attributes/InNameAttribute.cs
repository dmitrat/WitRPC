namespace OutWit.Examples.Services.ClientBlazor.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class InNameAttribute : Attribute
    {
        public string Name { get; }

        public InNameAttribute(string name)
        {
            Name = name;
        }
    }
}
