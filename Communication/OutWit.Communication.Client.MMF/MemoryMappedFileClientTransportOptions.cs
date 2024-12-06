using System;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Communication.Client.MMF
{
    public class MemoryMappedFileClientTransportOptions : ModelBase
    {
        #region Functions

        public override string ToString()
        {
            return $"Name: {Name}";
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (!(modelBase is MemoryMappedFileClientTransportOptions options))
                return false;

            return Name.Is(options.Name);
        }

        public override MemoryMappedFileClientTransportOptions Clone()
        {
            return new MemoryMappedFileClientTransportOptions
            {
                Name = Name
            };
        }

        #endregion

        #region Properties
        
        public string? Name { get; set; }

        #endregion
    }
}
