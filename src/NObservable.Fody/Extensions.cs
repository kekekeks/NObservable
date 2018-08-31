using Mono.Cecil;

namespace NObservable.Fody
{
    public static class Extensions
    {
        public static bool AreEqual(this TypeReference ref1, TypeReference ref2)
        {
            if (ref1.FullName != ref2.FullName)
                return false;
            if (ref1.Scope.MetadataScopeType != ref2.Scope.MetadataScopeType)
                return false;
            return ref1.Scope.MetadataToken.Equals(ref2.Scope.MetadataToken);
        }
    }
}