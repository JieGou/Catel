﻿//#define ENABLE_DETAILED_LOGGING

namespace Catel.Runtime.Serialization.Xml
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using Catel.Caching;
    using Catel.Logging;
    using Catel.Reflection;
    using Collections;

    /// <summary>
    /// Default implementation of the <see cref="IDataContractSerializerFactory" /> interface.
    /// </summary>
    public class DataContractSerializerFactory : IDataContractSerializerFactory
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly CacheStorage<string, IReadOnlyCollection<Type>> _knownTypesCache = new CacheStorage<string, IReadOnlyCollection<Type>>(storeNullValues: true);
        private readonly CacheStorage<string, DataContractSerializer> _dataContractSerializersCache = new CacheStorage<string, DataContractSerializer>(storeNullValues: true);
        private readonly CacheStorage<string, IReadOnlyCollection<Type>> _knownTypesByAttributesCache = new CacheStorage<string, IReadOnlyCollection<Type>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DataContractSerializerFactory"/> class.
        /// </summary>
        public DataContractSerializerFactory()
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="DataContractResolver"/> passed in constructor to <see cref="DataContractSerializer"/>.
        /// <para />
        /// The default value is <null/>.
        /// </summary>
        /// <value>The <see cref="DataContractResolver"/>.</value>
        public DataContractResolver? DataContractResolver { get; set; }

        /// <summary>
        /// Gets the known types for a specific type. This method caches the lists so the
        /// performance can be improved when a type is used more than once.
        /// </summary>
        /// <param name="serializingType">The type that is currently (de)serializing.</param>
        /// <param name="typeToSerialize">The type to (de)serialize.</param>
        /// <param name="additionalKnownTypes">A list of additional types to add to the known types.</param>
        /// <returns><see cref="DataContractSerializer" /> for the given type.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="serializingType" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="typeToSerialize" /> is <c>null</c>.</exception>
        public virtual IReadOnlyCollection<Type> GetKnownTypes(Type serializingType, Type typeToSerialize, IReadOnlyCollection<Type>? additionalKnownTypes = null)
        {
            var key = CreateCacheKey(serializingType, typeToSerialize, string.Empty, additionalKnownTypes);

            return _knownTypesCache.GetFromCacheOrFetch(key, () =>
            {
#if ENABLE_DETAILED_LOGGING
                Log.Debug("Getting known types for xml serialization of '{0}'", typeToSerializeName);
#endif

                var serializerTypeInfo = new XmlSerializerTypeInfo(serializingType, typeToSerialize, additionalKnownTypes);

                GetKnownTypes(typeToSerialize, serializerTypeInfo);

                var knownTypesViaAttributes = GetKnownTypesViaAttributes(serializingType);
                foreach (var knownTypeViaAttribute in knownTypesViaAttributes)
                {
                    GetKnownTypes(knownTypeViaAttribute, serializerTypeInfo);
                }

                if (additionalKnownTypes is not null)
                {
                    foreach (var additionalKnownType in additionalKnownTypes)
                    {
                        GetKnownTypes(additionalKnownType, serializerTypeInfo);
                    }
                }

                // Fixed known types
                GetKnownTypes(typeof(SerializableKeyValuePair), serializerTypeInfo);

                return serializerTypeInfo.KnownTypes;
            });
        }

        /// <summary>
        /// Gets the Data Contract serializer for a specific type. This method caches serializers so the
        /// performance can be improved when a serializer is used more than once.
        /// </summary>
        /// <param name="serializingType">The type that is currently (de)serializing.</param>
        /// <param name="typeToSerialize">The type to (de)serialize.</param>
        /// <param name="xmlName">Name of the property as known in XML.</param>
        /// <param name="rootNamespace">The root namespace.</param>
        /// <param name="additionalKnownTypes">A list of additional types to add to the known types.</param>
        /// <returns><see cref="DataContractSerializer" /> for the given type.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="serializingType" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="typeToSerialize" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The <paramref name="xmlName" /> is <c>null</c> or whitespace.</exception>
        public virtual DataContractSerializer GetDataContractSerializer(Type serializingType, Type typeToSerialize, string xmlName, string? rootNamespace = null, IReadOnlyCollection<Type>? additionalKnownTypes = null)
        {
            Argument.IsNotNullOrWhitespace("xmlName", xmlName);

            var key = CreateCacheKey(serializingType, typeToSerialize, xmlName, additionalKnownTypes);

            return _dataContractSerializersCache.GetFromCacheOrFetch(key, () =>
            {
                var knownTypes = GetKnownTypes(serializingType, typeToSerialize, additionalKnownTypes);
                var xmlSerializer = new DataContractSerializer(typeToSerialize, xmlName, rootNamespace ?? string.Empty, knownTypes);
                return xmlSerializer;
            });
        }

        protected virtual string CreateCacheKey(Type serializingType, Type typeToSerialize, string xmlName, IReadOnlyCollection<Type>? additionalKnownTypes)
        {
            var serializingTypeName = serializingType.GetSafeFullName(false);
            var typeToSerializeName = typeToSerialize.GetSafeFullName(false);

            var additionalTypes = string.Empty;

            if (additionalKnownTypes is not null)
            {
                additionalTypes = string.Join(";", additionalKnownTypes.Select(x => x.Name));
            }

            var key = $"{serializingTypeName}|{typeToSerializeName}|{additionalTypes}|{xmlName}";
            return key;
        }

        /// <summary>
        /// Gets the known types inside the specific type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="serializerTypeInfo">The serializer type info.</param>
        /// <param name="resolveAbstractClassesAndInterfaces">if set to <c>true</c> [resolve abstract classes and interfaces].</param>
        /// <returns>Array of <see cref="Type" /> that are found in the object type.</returns>
        protected virtual void GetKnownTypes(Type type, XmlSerializerTypeInfo serializerTypeInfo, bool resolveAbstractClassesAndInterfaces = true)
        {
            if (ShouldTypeBeIgnored(type, serializerTypeInfo))
            {
                return;
            }

#if ENABLE_DETAILED_LOGGING
            Log.Debug("Getting known types for '{0}'", type.GetSafeFullName(false));
#endif

            GetKnownTypesForItems(type, serializerTypeInfo);

            // If this is an interface or abstract, we need to retrieve all items that might possible implement or derive
            var isInterface = type.IsInterfaceEx();
            var isAbstract = type.IsAbstractEx();
            if (isInterface || isAbstract)
            {
                if (!serializerTypeInfo.IsTypeAlreadyHandled(type) && resolveAbstractClassesAndInterfaces)
                {
                    // Interfaces / abstract classes are not a type, and in fact a LOT of types can be added (in fact every object implementing 
                    // the interface). For serialization, this is not a problem (we know the exact type), but for deserialization this IS an 
                    // issue because we should expect EVERY type that implements the type in the whole AppDomain.
                    // This is huge performance hit, but it's the cost for dynamic easy on-the-fly serialization in WPF. Luckily
                    // we already implemented caching.

                    // Don't check this type again in children checks
                    serializerTypeInfo.AddTypeAsHandled(type);

#if ENABLE_DETAILED_LOGGING
                    Log.Debug("Type is an interface / abstract class, checking all types implementing / deriving");
#endif

                    if (isInterface)
                    {
                        var typesImplementingInterface = TypeCache.GetTypesImplementingInterface(type);
                        foreach (var typeImplementingInterface in typesImplementingInterface)
                        {
                            if (typeImplementingInterface != type)
                            {
                                GetKnownTypes(typeImplementingInterface, serializerTypeInfo);
                            }
                        }
                    }

                    if (isAbstract)
                    {
                        var typesDerivingFromClass = TypeCache.GetTypes(type.IsAssignableFromEx, false);
                        foreach (var typeDerivingFromClass in typesDerivingFromClass)
                        {
                            if (typeDerivingFromClass != type)
                            {
                                GetKnownTypes(typeDerivingFromClass, serializerTypeInfo);
                            }
                        }
                    }

#if ENABLE_DETAILED_LOGGING
                    Log.Debug("Finished checking all types implementing / deriving");
#endif
                }

                // The interface itself is ignored
                return;
            }

            if (serializerTypeInfo.IsSpecialCollectionType(type) && !type.IsInterfaceEx())
            {
#if ENABLE_DETAILED_LOGGING
                Log.Debug("Type is a special collection type, adding it to the array of known types");
#endif

                AddTypeToKnownTypesIfSerializable(type, serializerTypeInfo);
            }

            // Fix generics
            if (type.GetSafeFullName(false).StartsWith("System."))
            {
                var genericArguments = type.GetGenericArgumentsEx();
                foreach (var genericArgument in genericArguments)
                {
#if ENABLE_DETAILED_LOGGING
                    Log.Debug("Retrieving known types for generic argument '{0}' of '{1}'", genericArgument.GetSafeFullName(false), type.GetSafeFullName(false));
#endif

                    GetKnownTypes(genericArgument, serializerTypeInfo);
                }

                return;
            }

            if (!AddTypeToKnownTypesIfSerializable(type, serializerTypeInfo))
            {
                serializerTypeInfo.AddTypeAsHandled(type);
            }

            AddTypeMembers(type, serializerTypeInfo);

            // If this isn't the base type, check that as well
            var baseType = type.GetBaseTypeEx();
            if (baseType is not null)
            {
#if ENABLE_DETAILED_LOGGING
                Log.Debug("Checking base type of '{0}' for known types", type.GetSafeFullName(false));
#endif

                if (baseType.FullName is not null)
                {
                    GetKnownTypes(baseType, serializerTypeInfo);
                }
                else
                {
                    serializerTypeInfo.AddTypeAsHandled(baseType);
                }
            }

            // Last but not least, check if the type is decorated with KnownTypeAttributes
            var knowTypesByAttributes = GetKnownTypesViaAttributes(type);
            if (knowTypesByAttributes.Count > 0)
            {
#if ENABLE_DETAILED_LOGGING
                Log.Debug("Found {0} additional known types for type '{1}'", knowTypesByAttributes.Count, type.GetSafeFullName(false));
#endif

                foreach (var knownTypeByAttribute in knowTypesByAttributes)
                {
                    var attributeType = knownTypeByAttribute;
                    var attributeTypeFullName = attributeType.GetSafeFullName(false);
                    if (attributeTypeFullName is not null)
                    {
                        GetKnownTypes(knownTypeByAttribute, serializerTypeInfo);
                    }
                    else
                    {
                        serializerTypeInfo.AddTypeAsHandled(attributeType);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the known types of IEnumerable type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="serializerTypeInfo">The serializer type info.</param>
        /// <returns>Array of <see cref="Type"/> that are found in the object type.</returns>
        private void GetKnownTypesForItems(Type type, XmlSerializerTypeInfo serializerTypeInfo)
        {
            if (type.ImplementsInterfaceEx<IEnumerable>())
            {
                foreach (var argument in type.GetGenericArgumentsEx())
                {
                    GetKnownTypes(argument, serializerTypeInfo);
                }
            }
        }

        private void AddTypeMembers(Type type, XmlSerializerTypeInfo serializerTypeInfo)
        {
            var typesToCheck = new List<Type>();

            var isModelBase = type.IsModelBase();
            if (isModelBase)
            {
                // No need to check members, they will be serialized by ModelBase
                //var catelTypeInfo = PropertyDataManager.Default.GetCatelTypeInfo(type);
                //var modelBaseProperties = catelTypeInfo.GetCatelProperties();
                //foreach (var modelBaseProperty in modelBaseProperties)
                //{
                //    typesToCheck.Add(modelBaseProperty.Value.Type);
                //}
            }
            else
            {
                var allowNonPublicReflection = AllowNonPublicReflection(type);

                // Fields
                var fields = type.GetFieldsEx(BindingFlagsHelper.GetFinalBindingFlags(false, false, allowNonPublicReflection));
                foreach (var field in fields)
                {
                    typesToCheck.Add(field.FieldType);
                }

                // Properties
                var properties = type.GetPropertiesEx(BindingFlagsHelper.GetFinalBindingFlags(false, false, allowNonPublicReflection));
                foreach (var property in properties)
                {
                    typesToCheck.Add(property.PropertyType);
                }
            }

            foreach (var typeToCheck in typesToCheck)
            {
                if (serializerTypeInfo.IsTypeAlreadyHandled(typeToCheck))
                {
                    continue;
                }

                if (!IsTypeSerializable(typeToCheck, serializerTypeInfo))
                {
                    serializerTypeInfo.AddTypeAsHandled(typeToCheck);
                    continue;
                }

                var propertyTypeFullName = typeToCheck.GetSafeFullName(false);
                if (propertyTypeFullName is null)
                {
                    serializerTypeInfo.AddTypeAsHandled(typeToCheck);
                    continue;
                }

                GetKnownTypes(typeToCheck, serializerTypeInfo);
            }
        }

        /// <summary>
        /// Determines whether the specified type is serializable.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="serializerTypeInfo">The serializer type information.</param>
        /// <returns><c>true</c> if the specified type is serializable; otherwise, <c>false</c>.</returns>
        protected virtual bool IsTypeSerializable(Type type, XmlSerializerTypeInfo serializerTypeInfo)
        {
            // DataContract attribute
            if (type.IsDecoratedWithAttribute<DataContractAttribute>())
            {
                return true;
            }

            // Implements ISerializable
            if (type.ImplementsInterfaceEx<ISerializable>())
            {
                return true;
            }

            // Implements IXmlSerializer
            if (type.ImplementsInterfaceEx<System.Xml.Serialization.IXmlSerializable>())
            {
                return true;
            }

            // Is ModelBase
            if (type.IsModelBase())
            {
                return true;
            }

            // IsSerializable
            return type.IsSerializableEx();
        }

        /// <summary>
        /// Determines whether the type should be handled.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="serializerTypeInfo">The serializer type info.</param>
        /// <returns><c>true</c> if the type should be handled; otherwise, <c>false</c>.</returns>
        protected virtual bool ShouldTypeBeIgnored(Type type, XmlSerializerTypeInfo serializerTypeInfo)
        {
            // Never include generic type definitions, otherwise we will get this:
            // Error while getting known types for Type 'Catel.Tests.Data.PropertyDataManagerFacts+SupportsGenericClasses+GenericClass`1[T]'. The type must not be an open or partial generic class.
            if (type.IsGenericTypeDefinitionEx())
            {
                return true;
            }

            // Note, although resharper says this isn't possible, it might be
            var fullName = type.GetSafeFullName(false);
            if (string.IsNullOrWhiteSpace(fullName))
            {
                serializerTypeInfo.AddTypeAsHandled(type);
                return true;
            }

            // Ignore non-generic .NET
            if (!type.IsGenericTypeEx() && fullName.StartsWith("System."))
            {
                // Log.Debug("Non-generic .NET system type, can be ignored");
                serializerTypeInfo.AddTypeAsHandled(type);
                return true;
            }

            if (type.IsCOMObjectEx())
            {
                serializerTypeInfo.AddTypeAsHandled(type);
                return true;
            }

            return serializerTypeInfo.ContainsKnownType(type) ||
                   serializerTypeInfo.IsTypeAlreadyHandled(type) ||
                   serializerTypeInfo.IsCollectionAlreadyHandled(type);
        }

        /// <summary>
        /// Gets the known types via attributes.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The list of known types via the <see cref="KnownTypeAttribute"/>.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="type"/> is <c>null</c>.</exception>
        protected virtual IReadOnlyCollection<Type> GetKnownTypesViaAttributes(Type type)
        {
            var typeName = type.AssemblyQualifiedName;
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return Array.Empty<Type>();
            }

            return _knownTypesByAttributesCache.GetFromCacheOrFetch(typeName, () =>
            {
                var additionalTypes = new List<Type>();
                var knownTypeAttributes = type.GetCustomAttributesEx(typeof(KnownTypeAttribute), true);

                foreach (var attr in knownTypeAttributes)
                {
                    var ktattr = attr as KnownTypeAttribute;
                    if (ktattr is not null)
                    {
                        if (ktattr.MethodName is not null)
                        {
                            var mi = type.GetMethodEx(ktattr.MethodName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

                            // this can be null because we are also getting here through the recursive behaviour
                            // of GetCustomAttributesEx. We are getting at this point once per class derived from a
                            // base class having a KnownType() with a method. This can be ignored
                            if (mi is not null)
                            {
                                var types = mi.Invoke(null, null) as IEnumerable<Type>;
                                if (types is not null)
                                {
                                    additionalTypes.AddRange(types);
                                }
                            }
                        }
                        else
                        {
                            if (ktattr.Type is not null)
                            {
                                additionalTypes.Add(ktattr.Type);
                            }
                        }
                    }
                }

                return additionalTypes;
            });
        }

        /// <summary>
        /// Adds the type to the known types if the type is serializable.
        /// </summary>
        /// <param name="typeToAdd">The type to add.</param>
        /// <param name="serializerTypeInfo">The serializer type info.</param>
        /// <returns><c>true</c> if the type is serializable; otherwise <c>false</c>.</returns>
        protected virtual bool AddTypeToKnownTypesIfSerializable(Type typeToAdd, XmlSerializerTypeInfo serializerTypeInfo)
        {
            // Collection first, this collection of types is smaller so if we have a hit, we exit sooner
            if (serializerTypeInfo.IsCollectionAlreadyHandled(typeToAdd))
            {
                return true;
            }

            if (serializerTypeInfo.ContainsKnownType(typeToAdd))
            {
                return true;
            }

            serializerTypeInfo.AddCollectionAsHandled(typeToAdd);

            // If this is a special collection type (generic), then we need to make sure that if the inner type is
            // an interface, we do not add it again if already added.
            // See this issue http://catel.codeplex.com/workitem/7167
            if (serializerTypeInfo.IsSpecialCollectionType(typeToAdd))
            {
                // Always ignore
                return false;
            }

            return serializerTypeInfo.AddKnownType(typeToAdd);
        }

        /// <summary>
        /// Returns whether non-public reflection is allowed on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if non-public reflection is allowed, <c>false</c> otherwise.</returns>
        protected virtual bool AllowNonPublicReflection(Type type)
        {
            var allowNonPublicReflection = type.IsModelBase();

            return allowNonPublicReflection;
        }

        /// <summary>
        /// Class containing serializer type info.
        /// </summary>
        protected class XmlSerializerTypeInfo
        {
            private readonly HashSet<Type> _collectionTypesAlreadyHandled = new HashSet<Type>();
            private readonly Dictionary<Type, bool> _isSpecialCollectionCache = new Dictionary<Type, bool>();

            private readonly HashSet<Type> _knownTypes = new HashSet<Type>();
            private readonly HashSet<Type> _specialCollectionTypes = new HashSet<Type>();
            private readonly HashSet<Type> _specialGenericCollectionTypes = new HashSet<Type>();
            private readonly HashSet<Type> _typesAlreadyHandled = new HashSet<Type>();

            private readonly CacheStorage<Type, bool> _isTypeSerializableCache = new CacheStorage<Type, bool>();

            /// <summary>
            /// Initializes a new instance of the <see cref="XmlSerializerTypeInfo" /> class.
            /// </summary>
            /// <param name="serializingType">Type of the serializing.</param>
            /// <param name="typeToSerialize">The type to serialize.</param>
            /// <param name="additionalKnownTypes">The additional known types.</param>
            public XmlSerializerTypeInfo(Type serializingType, Type typeToSerialize, IEnumerable<Type>? additionalKnownTypes = null)
            {
                SerializingType = serializingType;
                TypeToSerialize = typeToSerialize;

                if (additionalKnownTypes is not null)
                {
                    _knownTypes.AddRange(additionalKnownTypes);
                }
            }

            /// <summary>
            /// Gets the serializing type.
            /// </summary>
            /// <value>The serializing type.</value>
            public Type SerializingType { get; private set; }

            /// <summary>
            /// Gets the type to serialize.
            /// </summary>
            /// <value>The type to serialize.</value>
            public Type TypeToSerialize { get; private set; }

            /// <summary>
            /// Gets the known types.
            /// </summary>
            /// <value>The known types.</value>
            public IReadOnlyCollection<Type> KnownTypes
            {
                get { return _knownTypes; }
            }

            /// <summary>
            /// Gets the types already handled.
            /// </summary>
            /// <value>The types already handled.</value>
            public IReadOnlyCollection<Type> TypesAlreadyHandled
            {
                get { return _typesAlreadyHandled; }
            }

            /// <summary>
            /// Gets the special collection types.
            /// </summary>
            /// <value>The special collection types.</value>
            public IReadOnlyCollection<Type> SpecialCollectionTypes
            {
                get { return _specialCollectionTypes; }
            }

            /// <summary>
            /// Gets the special generic collection types.
            /// </summary>
            /// <value>The special generic collection types.</value>
            public IReadOnlyCollection<Type> SpecialGenericCollectionTypes
            {
                get { return _specialGenericCollectionTypes; }
            }

            /// <summary>
            /// Adds the type to the list of known types.
            /// </summary>
            /// <param name="type">The type.</param>
            public bool AddKnownType(Type type)
            {
                if (!IsTypeSerializable(type))
                {
                    return false;
                }

                _knownTypes.Add(type);

                if (IsSpecialCollectionType(type))
                {
                    _specialCollectionTypes.Add(type);

                    if (type.IsGenericTypeEx())
                    {
                        _specialGenericCollectionTypes.Add(type);
                    }
                }

                return true;
            }

            /// <summary>
            /// Determines whether the specified type is a known type.
            /// </summary>
            /// <param name="type">The type.</param>
            /// <returns><c>true</c> if the specified type is known type; otherwise, <c>false</c>.</returns>
            public bool ContainsKnownType(Type type)
            {
                return _knownTypes.Contains(type);
            }

            /// <summary>
            /// Adds the type as handled.
            /// </summary>
            /// <param name="type">The type.</param>
            public void AddTypeAsHandled(Type type)
            {
                _typesAlreadyHandled.Add(type);
            }

            /// <summary>
            /// Determines whether the specified type is already handled, which doesn't mean that it is also
            /// a known type. It means that the type has already been inspected once.
            /// </summary>
            /// <param name="type">The type.</param>
            /// <returns><c>true</c> if the type is already handled; otherwise, <c>false</c>.</returns>
            public bool IsTypeAlreadyHandled(Type type)
            {
                return _typesAlreadyHandled.Contains(type);
            }

            /// <summary>
            /// Adds the collection type as handled.
            /// </summary>
            /// <param name="type">The type.</param>
            public void AddCollectionAsHandled(Type type)
            {
                _collectionTypesAlreadyHandled.Add(type);
            }

            /// <summary>
            /// Determines whether the specified collection type is already handled.
            /// </summary>
            /// <param name="type">The type.</param>
            /// <returns><c>true</c> if the collection type is already handled; otherwise, <c>false</c>.</returns>
            public bool IsCollectionAlreadyHandled(Type type)
            {
                return _collectionTypesAlreadyHandled.Contains(type);
            }

            /// <summary>
            /// Determines whether the specified type is a special .NET collection type which should be
            /// added to the serialization known types.
            /// <para />
            /// All generic collections in the <c>System.Collections.Generic</c> namespace are considered
            /// special. Besides these classes, the <c>ObservableCollection{T}</c> is also considered
            /// special.
            /// </summary>
            /// <param name="type">The type.</param>
            /// <returns><c>true</c> if the specified type is a special collection type; otherwise, <c>false</c>.</returns>
            /// <exception cref="ArgumentNullException">The <paramref name="type" /> is <c>null</c>.</exception>
            public bool IsSpecialCollectionType(Type type)
            {
                if (_isSpecialCollectionCache.TryGetValue(type, out var isSpecialCollectionCached))
                {
                    return isSpecialCollectionCached;
                }

                // Check all sub types as well (a type might be deriving from IEnumerable)
                var baseType = type;
                while (baseType is not null)
                {
                    if (IsSpecificTypeSpecialCollection(baseType))
                    {
                        // Note: this is not a bug, we need to add type to the collection
                        _isSpecialCollectionCache[type] = true;
                        return true;
                    }

                    baseType = baseType.GetBaseTypeEx();
                }

                var implementedInterfaces = type.GetInterfacesEx();
                foreach (var implementedInterface in implementedInterfaces)
                {
                    if (IsSpecificTypeSpecialCollection(implementedInterface))
                    {
                        // Note: this is not a bug, we need to add type to the collection
                        _isSpecialCollectionCache[type] = true;
                        return true;
                    }
                }

                _isSpecialCollectionCache[type] = false;
                return false;
            }

            /// <summary>
            /// Determines whether the specified type is serializable.
            /// </summary>
            /// <param name="type">The type.</param>
            /// <returns><c>true</c> if the specified type is serializable; otherwise, <c>false</c>.</returns>
            /// <exception cref="ArgumentNullException">The <paramref name="type" /> is <c>null</c>.</exception>
            public bool IsTypeSerializable(Type type)
            {
                return _isTypeSerializableCache.GetFromCacheOrFetch(type, () =>
                {
                    if (type.IsAbstractEx())
                    {
                        return false;
                    }

                    if (type.IsInterfaceEx())
                    {
                        return false;
                    }

                    if (type.IsEnumEx())
                    {
                        return true;
                    }

                    if (IsSpecialCollectionType(type))
                    {
                        return true;
                    }

                    // Should have an empty constructor
                    if (type.GetConstructorEx(Array.Empty<Type>()) is null)
                    {
                        return false;
                    }

                    // Type must be public
                    if (!type.IsPublicEx() && !type.IsNestedPublicEx())
                    {
                        return false;
                    }

                    // TODO: Add more checks?

                    return true;
                });
            }

            private bool IsSpecificTypeSpecialCollection(Type type)
            {
                if (type.IsGenericTypeEx())
                {
                    var fullName = type.GetSafeFullName(false);

                    if (fullName.StartsWith("System.Collections.ObjectModel.ObservableCollection") ||
                        fullName.StartsWith("System.Collections.Generic."))
                    {
                        // Note: this is not a bug, we need to add type to the collection
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
