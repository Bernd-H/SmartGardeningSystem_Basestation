using System;
using System.Runtime.InteropServices;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Utilities;

namespace GardeningSystem.Common {
    public class SecureMemory : ISecureMemory {

        private PointerLengthPair _plp;

        private GCHandle _gchOfSecureObject;

        private byte[] _secureObject;

        //public byte[] Object {
        //    get {
        //        if (_secureObject != null) {
        //            throw new Exception($"The secure object has already been requested.");
        //        }

        //        _gchOfSecureObject = CryptoUtils.GetByteArrayFromUM(ref _secureObject, _plp);

        //        return _secureObject;
        //    }
        //}

        public byte[] GetObject() {
            if (_secureObject != null) {
                throw new Exception($"The secure object has already been requested.");
            }

            _gchOfSecureObject = CryptoUtils.GetByteArrayFromUM(ref _secureObject, _plp);

            return _secureObject;
        }

        public SecureMemory() {
            _plp = null;
            _secureObject = null;
        }

        public SecureMemory(PointerLengthPair plp) {
            _plp = plp;
        }

        public void LoadObject(PointerLengthPair plp) {
            _plp = plp;
        }

        public void Dispose() {
            CryptoUtils.ObfuscateByteArray(_secureObject, _gchOfSecureObject);
            _secureObject = null;
            _plp = null;

            // does not free the memory where the pointer from plp points to
        }
    }
}
