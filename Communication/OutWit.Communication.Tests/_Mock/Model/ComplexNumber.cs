using MessagePack;
using OutWit.Common.Abstract;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using MemoryPack;
using ProtoBuf;

namespace OutWit.Communication.Tests.Mock.Model
{
    [MessagePackObject]
    [DataContract]
    [MemoryPackable]
    [ProtoContract]
    public partial class ComplexNumber<T1, T2> : ModelBase
    {

        private ComplexNumber()
        {
            
        }

        [MemoryPackConstructor]
        [JsonConstructor]
        [SerializationConstructor]
        public ComplexNumber(T1 a, T2 b)
        {
            A = a;
            B = b;
        }

        public override string ToString()
        {
            return $"A: {A}, B: {B}";
        }

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is ComplexNumber<T1, T2> request))
                return false;

            return A.Equals(request.A) &&
                   B.Equals(request.B);
        }

        public override ModelBase Clone()
        {
            return new ComplexNumber<T1, T2>(A, B);
        }


        [Key(0)]
        [DataMember]
        [ProtoMember(1)]
        public T1 A { get; set; }
        [Key(1)]
        [DataMember]
        [ProtoMember(2)]
        public T2 B { get; set; }

    }
}
