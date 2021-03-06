﻿namespace Microsoft.OutlookServices
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Exchange.RestServices;

    /// <summary>
    /// Partial entity.
    /// </summary>
    public abstract partial class Entity : IPropertyChangeTracking
    {
        private ExchangeService service;

        /// <summary>
        /// Property bag.
        /// </summary>
        protected PropertyBag propertyBag;

        /// <summary>
        /// Entity.
        /// </summary>
        protected Entity()
        {
            Type schemaType = Assembly.GetExecutingAssembly().GetType(
                this.GetType().FullName + "ObjectSchema");
            if (schemaType != null)
            {
                object instance = Activator.CreateInstance(schemaType);
                if (instance is ObjectSchema)
                {
                    this.propertyBag = new PropertyBag(instance as ObjectSchema);
                }
            }
            else
            {
                throw new NullReferenceException(
                    $"Cannot find schema definition '{this.GetType().FullName}ObjectSchema'.");
            }
        }

        /// <summary>
        /// When entity is created outside this ..ctor needs to be used.
        /// </summary>
        /// <param name="service"></param>
        protected Entity(ExchangeService service)
            : this()
        {
            this.service = service;
            this.propertyBag.MarkAsNew();
        }

        /// <summary>
        /// When data retrieved from the server, service needs to be set
        /// in order for a method invocation to work.
        /// </summary>
        internal ExchangeService Service
        {
            get { return this.service; }
            set { this.service = value; }
        }

        /// <summary>
        /// Indicate if bag is new.
        /// </summary>
        internal bool IsNew
        {
            get { return this.propertyBag.IsNew; }
        }

        /// <summary>
        /// Mailbox id.
        /// </summary>
        internal MailboxId MailboxId { get; set; }

        /// <summary>
        /// Get a list of changed properties.
        /// </summary>
        /// <returns></returns>
        public IList<string> GetChangedPropertyNames()
        {
            return this.propertyBag.GetChangedPropertyNames();
        }

        /// <summary>
        /// Get list of changed properties.
        /// </summary>
        /// <returns></returns>
        public IList<PropertyDefinition> GetChangedProperies()
        {
            return this.propertyBag.GetChangedProperies();
        }

        /// <summary>
        /// Indexer.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object this[string key]
        {
            get { return this.propertyBag[key]; }
        }

        /// <summary>
        /// Indexer.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object this[PropertyDefinition key]
        {
            get { return this.propertyBag[key]; }
        }

        /// <summary>
        /// Reset change tracking.
        /// </summary>
        internal void ResetChangeTracking()
        {
            this.propertyBag.ResetChangeTracking();
        }

        /// <summary>
        /// Validates if update can occur.
        /// </summary>
        protected void PreValidateUpdate()
        {
            if (this.IsNew)
            {
                throw new ArgumentException(
                    "Cannot perform update on newly created item. Sync item from server and try again.");
            }

            if (this.propertyBag.GetChangedPropertyNames().Count == 0)
            {
                throw new ArgumentException("No changed properties detected.");
            }

            ArgumentValidator.ThrowIfNull(
                this.Service,
                nameof(this.Service));
        }

        /// <summary>
        /// Pre validate save.
        /// </summary>
        protected void PreValidateSave()
        {
            if (!this.IsNew)
            {
                throw new ArgumentException("Cannot call 'Save' on existing object.");
            }

            ArgumentValidator.ThrowIfNull(
                this.Service,
                nameof(this.Service));
        }

        /// <summary>
        /// Pre validate delete.
        /// </summary>
        protected void PreValidateDelete()
        {
            if (this.IsNew)
            {
                throw new ArgumentException(
                    "Cannot perform delete operation on non existing object. Sync folder with server and try again.");
            }

            ArgumentValidator.ThrowIfNull(
                this.Service,
                nameof(this.Service));
        }
    }

    /// <summary>
    /// Directory object.
    /// </summary>
    public abstract partial class DirectoryObject
    {
        internal DirectoryObject(ExchangeService exchangeService)
            : base(exchangeService)
        {
        }
    }

    /// <summary>
    /// User.
    /// </summary>
    public partial class User
    {
        /// <summary>
        /// Create new instance of <see cref="User"/>
        /// </summary>
        /// <param name="exchangeService"></param>
        /// <param name="mailboxId"></param>
        internal User(ExchangeService exchangeService, MailboxId mailboxId)
            : base(exchangeService)
        {
            this.MailboxId = mailboxId;
        }

        /// <summary>
        /// MailboxId.
        /// </summary>
        internal MailboxId MailboxId { get; }
    }

    /// <summary>
    /// Group.
    /// </summary>
    public partial class Group
    {
        /// <summary>
        /// Create new instance of <see cref="Group"/>
        /// </summary>
        /// <param name="exchangeService"></param>
        /// <param name="mailboxId"></param>
        internal Group(ExchangeService exchangeService, MailboxId mailboxId)
            : base(exchangeService)
        {
            this.MailboxId = mailboxId;
        }

        /// <summary>
        /// MailboxId.
        /// </summary>
        internal MailboxId MailboxId
        {
            get;
        }
    }

    /// <summary>
    /// Mail folder.
    /// </summary>
    public partial class MailFolder
    {
        /// <summary>
        /// Create new instance of <see cref="MailFolder"/>
        /// </summary>
        internal MailFolder()
            : base()
        {
        }

        /// <summary>
        /// Create new instance of <see cref="MailFolder"/>
        /// </summary>
        /// <param name="exchangeService"></param>
        public MailFolder(ExchangeService exchangeService)
            : base(exchangeService)
        {
        }

        /// <summary>
        /// Folder id.
        /// </summary>
        internal FolderId FolderId
        {
            get
            {
                if (this.IsNew)
                {
                    return null;
                }

                if (string.IsNullOrEmpty(this.Id))
                {
                    return null;
                }

                if (this.MailboxId == null)
                {
                    return new FolderId(this.Id);
                }

                return new FolderId(
                    this.Id,
                    this.MailboxId.Id);
            }
        }

        /// <summary>
        /// Bind to a particular folder.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="folderId"></param>
        /// <returns></returns>
        public static MailFolder Bind(ExchangeService service, FolderId folderId)
        {
            return service.GetFolder(folderId);
        }

        /// <summary>
        /// Update mail folder.
        /// </summary>
        public void Update()
        {
            this.PreValidateUpdate();
            MailFolder mailFolder = this.Service.UpdateFolder(this);
            this.propertyBag = mailFolder.propertyBag;
            this.MailboxId = mailFolder.MailboxId;
        }

        /// <summary>
        /// Save folder (Create new one).
        /// </summary>
        /// <param name="parentFolderId"></param>
        public void Save(FolderId parentFolderId)
        {
            this.PreValidateSave();
            ArgumentValidator.ThrowIfNullOrEmpty(this.DisplayName, nameof(this.DisplayName));
            ArgumentValidator.ThrowIfNull(parentFolderId, nameof(parentFolderId));

            MailFolder mailFolder = this.Service.CreateFolder(this, parentFolderId);
            this.propertyBag = mailFolder.propertyBag;
            this.MailboxId = mailFolder.MailboxId;
        }

        /// <summary>
        /// Save folder (Create new one).
        /// </summary>
        /// <param name="wellKnownFolderName"></param>
        public void Save(WellKnownFolderName wellKnownFolderName)
        {
            FolderId parentFolderId = new FolderId(wellKnownFolderName);
            this.Save(parentFolderId);
        }

        /// <summary>
        /// Delete folder.
        /// </summary>
        public void Delete()
        {
            this.PreValidateDelete();
            ArgumentValidator.ThrowIfNullOrEmpty(this.Id, nameof(this.Id));

            this.Service.DeleteFolder(this);
            this.propertyBag.Clear();
        }
    }

    /// <summary>
    /// Calendar folder.
    /// </summary>
    public partial class Calendar
    {
        /// <summary>
        /// Create new instance of <see cref="Calendar"/>
        /// </summary>
        internal Calendar()
            : base()
        {
        }

        /// <summary>
        /// Create new instance of <see cref="Calendar"/>
        /// </summary>
        /// <param name="exchangeService"></param>
        public Calendar(ExchangeService exchangeService)
            : base(exchangeService)
        {
        }

        /// <summary>
        /// Calendar FolderId.
        /// </summary>
        internal FolderId FolderId
        {
            get
            {
                if (this.IsNew)
                {
                    return null;
                }

                if (string.IsNullOrEmpty(this.Id))
                {
                    return null;
                }

                if (this.MailboxId == null)
                {
                    return new CalendarFolderId(this.Id);
                }

                return new CalendarFolderId(
                    this.Id,
                    this.MailboxId.Id);
            }
        }
    }

    /// <summary>
    /// Outlook item partial.
    /// </summary>
    public abstract partial class Item
    {
        /// <summary>
        /// Create new instance of <see cref="OUtlookItem"/>
        /// </summary>
        /// <param name="service">Exchange service.</param>
        public Item(ExchangeService service)
            : base(service)
        {
        }

        /// <summary>
        /// Create new instance of <see cref="OutlookItem"/>
        /// </summary>
        internal Item()
        {
        }

        /// <summary>
        /// Type id this item implements.
        /// </summary>
        protected abstract Type IdType { get; }

        /// <summary>
        /// Item id.
        /// </summary>
        internal virtual ItemId ItemId
        {
            get
            {
                if (null == this.IdType)
                {
                    throw new NotImplementedException(this.GetType().FullName);
                }

                if (this.IsNew)
                {
                    return null;
                }

                if (string.IsNullOrEmpty(this.Id))
                {
                    return null;
                }

                if (this.MailboxId == null)
                {
                    return (ItemId) Activator.CreateInstance(
                        this.IdType,
                        this.Id,
                        MailboxId.Me);
                }

                return (ItemId) Activator.CreateInstance(
                    this.IdType,
                    this.Id,
                    this.MailboxId);
            }
        }

        /// <summary>
        /// Update outlook item - Async.
        /// </summary>
        /// <returns></returns>
        public async System.Threading.Tasks.Task UpdateAsync()
        {
            if (this.IsNew)
            {
                throw new ArgumentException(
                    "Cannot perform update on newly created item. Sync item from server and try again.");
            }

            if (this.propertyBag.GetChangedPropertyNames().Count == 0)
            {
                throw new ArgumentException("No changed properties detected.");
            }

            Item outlookItem = await this.Service.UpdateItemAsync(this);
            this.propertyBag = outlookItem.propertyBag;
            this.MailboxId = outlookItem.MailboxId;
        }

        /// <summary>
        /// Update outlook item.
        /// </summary>
        public void Update()
        {
            if (this.IsNew)
            {
                throw new ArgumentException(
                    "Cannot perform update on newly created item. Sync item from server and try again.");
            }

            if (this.propertyBag.GetChangedPropertyNames().Count == 0)
            {
                throw new ArgumentException("No changed properties detected.");
            }

            Item outlookItem = this.Service.UpdateItem(this);
            this.propertyBag = outlookItem.propertyBag;
            this.MailboxId = outlookItem.MailboxId;
        }

        /// <summary>
        /// Save item on the server - Async.
        /// </summary>
        /// <param name="parentFolderId">Parent folder id.</param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task SaveAsync(FolderId parentFolderId)
        {
            this.PreValidateSave();
            ArgumentValidator.ThrowIfNull(parentFolderId, nameof(parentFolderId));

            Item outlookItem = await this.Service.CreateItemAsync(
                this,
                parentFolderId);

            this.propertyBag = outlookItem.propertyBag;
            this.MailboxId = outlookItem.MailboxId;
        }

        /// <summary>
        /// Save item on the server.
        /// </summary>
        /// <param name="parentFolderId">Parent folder id.</param>
        /// <returns></returns>
        public void Save(FolderId parentFolderId)
        {
            this.PreValidateSave();
            ArgumentValidator.ThrowIfNull(parentFolderId, nameof(parentFolderId));

            Item outlookItem = this.Service.CreateItem(
                this,
                parentFolderId);

            this.propertyBag = outlookItem.propertyBag;
            this.MailboxId = outlookItem.MailboxId;
        }

        /// <summary>
        /// Save item on the server.
        /// </summary>
        /// <param name="wellKnownFolderName">Well known folder name.</param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task SaveAsync(WellKnownFolderName wellKnownFolderName)
        {
            FolderId parentFolderId = new FolderId(wellKnownFolderName);
            await this.SaveAsync(parentFolderId);
        }

        /// <summary>
        /// Save item on the server.
        /// </summary>
        /// <param name="wellKnownFolderName">Well known folder name.</param>
        /// <returns></returns>
        public void Save(WellKnownFolderName wellKnownFolderName)
        {
            FolderId parentFolderId = new FolderId(wellKnownFolderName);
            this.Save(parentFolderId);
        }

        /// <summary>
        /// Delete item - Async.
        /// </summary>
        public async System.Threading.Tasks.Task DeleteAsync()
        {
            this.PreValidateDelete();
            await this.Service.DeleteItemAsync(this.ItemId);
            this.propertyBag.Clear();
        }

        /// <summary>
        /// Delete item.
        /// </summary>
        public void Delete()
        {
            this.PreValidateDelete();
            this.Service.DeleteItem(this.ItemId);
            this.propertyBag.Clear();
        }
    }

    /// <summary>
    /// Message item partial.
    /// </summary>
    public partial class Message
    {
        /// <summary>
        /// Create new instance of <see cref="Message"/>
        /// </summary>
        /// <param name="service">Exchange service.</param>
        public Message(ExchangeService service)
            : base(service)
        {
        }

        internal Message()
        {
        }

        /// <summary>
        /// Binds to a message.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public static Message Bind(ExchangeService service, ItemId itemId)
        {
            ArgumentValidator.ThrowIfNull(service, nameof(service));
            ArgumentValidator.ThrowIfNull(itemId, nameof(itemId));

            return service.GetItem(itemId);
        }

        /// <summary>
        /// Id type.
        /// </summary>
        protected override Type IdType
        {
            get { return typeof(MessageId); }
        }
    }

    /// <summary>
    /// Event item partial.
    /// </summary>
    public partial class Event
    {
        /// <summary>
        /// Create new instance of <see cref="Event"/>
        /// </summary>
        /// <param name="service">Exchange service.</param>
        public Event(ExchangeService service)
            : base(service)
        {
        }

        internal Event()
            : base()
        {
        }

        /// <summary>
        /// Id type.
        /// </summary>
        protected override Type IdType
        {
            get { return typeof(EventId); }
        }
    }

    /// <summary>
    /// Task item partial.
    /// </summary>
    public partial class Task
    {
        /// <summary>
        /// Create new instance of <see cref="Task"/>
        /// </summary>
        /// <param name="service">Exchange service.</param>
        public Task(ExchangeService service)
            : base(service)
        {
        }

        internal Task()
            : base()
        {
        }

        /// <summary>
        /// Id type.
        /// </summary>
        protected override Type IdType
        {
            get { return typeof(TaskId); }
        }
    }

    /// <summary>
    /// Contact item partial.
    /// </summary>
    public partial class Contact
    {
        /// <summary>
        /// Create new instance of <see cref="Contact"/>
        /// </summary>
        internal Contact()
            : base()
        {
        }

        /// <summary>
        /// Create new instance of <see cref="Contact"/>
        /// </summary>
        /// <param name="service">Exchange service.</param>
        public Contact(ExchangeService service)
            : base(service)
        {
        }

        /// <summary>
        /// Id type.
        /// </summary>
        protected override Type IdType
        {
            get { return typeof(ContactId); }
        }
    }

    /// <summary>
    /// Post item partial.
    /// </summary>
    public partial class Post
    {
        /// <summary>
        /// Create new instance of <see cref="Post"/>
        /// </summary>
        /// <param name="service">Exchange service.</param>
        public Post(ExchangeService service)
            : base(service)
        {
        }

        /// <summary>
        /// Id type.
        /// </summary>
        protected override Type IdType { get; }
    }

    /// <summary>
    /// SingleValueLegacyExtendedProperty
    /// </summary>
    public partial class SingleValueLegacyExtendedProperty : IPropertyChangeTracking
    {
        /// <summary>
        /// Property bag.
        /// </summary>
        protected PropertyBag propertyBag;

        /// <summary>
        /// Create new instance of <see cref="SingleValueLegacyExtendedProperty"/>
        /// </summary>
        internal SingleValueLegacyExtendedProperty()
        {
            Type schemaType = Assembly.GetExecutingAssembly().GetType(
                this.GetType().FullName + "ObjectSchema");
            if (schemaType != null)
            {
                object instance = Activator.CreateInstance(schemaType);
                if (instance is ObjectSchema)
                {
                    this.propertyBag = new PropertyBag(instance as ObjectSchema);
                }
            }
            else
            {
                throw new NullReferenceException(
                    $"Cannot find schema definition '{this.GetType().FullName}ObjectSchema'.");
            }
        }

        /// <summary>
        /// Get changed properties.
        /// </summary>
        /// <returns></returns>
        public IList<string> GetChangedPropertyNames()
        {
            return this.propertyBag.GetChangedPropertyNames();
        }

        /// <summary>
        /// Get changed properties.
        /// </summary>
        /// <returns></returns>
        public IList<PropertyDefinition> GetChangedProperies()
        {
            return this.propertyBag.GetChangedProperies();
        }

        /// <summary>
        /// Property indexer.
        /// </summary>
        /// <param name="key">Property key.</param>
        /// <returns></returns>
        public object this[string key]
        {
            get { return this.propertyBag[key]; }
        }

        /// <summary>
        /// Property indexer.
        /// </summary>
        /// <param name="key">Property key.</param>
        /// <returns></returns>
        public object this[PropertyDefinition key]
        {
            get { return this.propertyBag[key]; }
        }
    }

    /// <summary>
    /// MultiValueLegacyExtendedProperty
    /// </summary>
    public partial class MultiValueLegacyExtendedProperty : IPropertyChangeTracking
    {
        /// <summary>
        /// Property bag.
        /// </summary>
        protected PropertyBag propertyBag;

        /// <summary>
        /// Create new instance of <see cref="MultiValueLegacyExtendedProperty"/>
        /// </summary>
        internal MultiValueLegacyExtendedProperty()
        {
            Type schemaType = Assembly.GetExecutingAssembly().GetType(
                this.GetType().FullName + "ObjectSchema");
            if (schemaType != null)
            {
                object instance = Activator.CreateInstance(schemaType);
                if (instance is ObjectSchema)
                {
                    this.propertyBag = new PropertyBag(instance as ObjectSchema);
                }
            }
            else
            {
                throw new NullReferenceException(
                    $"Cannot find schema definition '{this.GetType().FullName}ObjectSchema'.");
            }
        }

        /// <summary>
        /// Get changed properties.
        /// </summary>
        /// <returns></returns>
        public IList<string> GetChangedPropertyNames()
        {
            return this.propertyBag.GetChangedPropertyNames();
        }

        /// <summary>
        /// Get changed properties.
        /// </summary>
        /// <returns></returns>
        public IList<PropertyDefinition> GetChangedProperies()
        {
            return this.propertyBag.GetChangedProperies();
        }

        /// <summary>
        /// Property indexer.
        /// </summary>
        /// <param name="key">Property key.</param>
        /// <returns></returns>
        public object this[string key]
        {
            get { return this.propertyBag[key]; }
        }

        /// <summary>
        /// Property indexer.
        /// </summary>
        /// <param name="key">Property key.</param>
        /// <returns></returns>
        public object this[PropertyDefinition key]
        {
            get { return this.propertyBag[key]; }
        }
    }

    /// <summary>
    /// Message rule partial.
    /// </summary>
    public partial class MessageRule
    {
        /// <summary>
        /// Create new instance of <see cref="MessageRule"/>
        /// <param name="service">Exchange service.</param>
        /// </summary>
        public MessageRule(ExchangeService service)
            : base(service)
        {
        }

        /// <summary>
        /// Create new instance of <see cref="MessageRule"/>
        /// </summary>
        internal MessageRule()
            : base()
        {
        }

        /// <summary>
        /// Update inbox rule - Async.
        /// </summary>
        public async System.Threading.Tasks.Task UpdateAsync()
        {
            this.PreValidateUpdate();
            MessageRule rule = await this.Service.UpdateInboxRuleAsync(this);
            this.propertyBag = rule.propertyBag;
            this.MailboxId = rule.MailboxId;
        }

        /// <summary>
        /// Update inbox rule.
        /// </summary>
        public void Update()
        {
            this.PreValidateUpdate();
            MessageRule rule = this.Service.UpdateInboxRule(this);
            this.propertyBag = rule.propertyBag;
            this.MailboxId = rule.MailboxId;
        }

        /// <summary>
        /// Delete inbox rule in async fashion.
        /// </summary>
        /// <returns></returns>
        public async System.Threading.Tasks.Task DeleteAsync()
        {
            this.PreValidateDelete();
            ArgumentValidator.ThrowIfNullOrEmpty(this.Id, nameof(this.Id));

            await this.Service.DeleteInboxRuleAsync(this);
            this.propertyBag.Clear();

        }

        /// <summary>
        /// Delete inbox rule.
        /// </summary>
        public void Delete()
        {
            this.PreValidateDelete();
            ArgumentValidator.ThrowIfNullOrEmpty(this.Id, nameof(this.Id));

            this.Service.DeleteInboxRule(this);
            this.propertyBag.Clear();
        }

        /// <summary>
        /// Save message rule - Async.
        /// </summary>
        /// <returns></returns>
        public async System.Threading.Tasks.Task SaveAsync()
        {
            this.PreValidateSave();
            MessageRule rule = await this.Service.CreateInboxRuleAsync(this);
            this.propertyBag = rule.propertyBag;
            this.MailboxId = rule.MailboxId;
        }

        /// <summary>
        /// Save message rule.
        /// </summary>
        /// <returns></returns>
        public void Save()
        {
            this.PreValidateSave();
            MessageRule rule = this.Service.CreateInboxRule(this);
            this.propertyBag = rule.propertyBag;
            this.MailboxId = rule.MailboxId;
        }
    }

    /// <summary>
    /// Inference classification override.
    /// </summary>
    public partial class InferenceClassificationOverride
    {
        /// <summary>
        /// Create new instance of <see cref="InferenceClassificationOverride"/>
        /// </summary>
        internal InferenceClassificationOverride()
            : base()
        {
        }

        /// <summary>
        /// Create new instance of <see cref="InferenceClassificationOverride"/>
        /// </summary>
        /// <param name="exchangeService">Exchange service.</param>
        public InferenceClassificationOverride(ExchangeService exchangeService)
            : base(exchangeService)
        {
        }

        /// <summary>
        /// Update override. 
        /// </summary>
        public async System.Threading.Tasks.Task UpdateAsync()
        {
            this.PreValidateUpdate();
            InferenceClassificationOverride inferenceClassificationOverride = await this.Service.UpdateInferenceClassificationOverrideAsync(this);
            this.propertyBag = inferenceClassificationOverride.propertyBag;
        }

        /// <summary>
        /// Update override. 
        /// </summary>
        public void Update()
        {
            this.PreValidateUpdate();
            InferenceClassificationOverride inferenceClassificationOverride = this.Service.UpdateInferenceClassificationOverride(this);
            this.propertyBag = inferenceClassificationOverride.propertyBag;
        }

        public async System.Threading.Tasks.Task SaveAsync()
        {
            this.PreValidateSave();
            InferenceClassificationOverride inferenceClassificationOverride = await this.Service.CreateInferenceClassificationOverrideAsync(this);
            this.propertyBag = inferenceClassificationOverride.propertyBag;
        }

        /// <summary>
        /// Create override.
        /// </summary>
        public void Save()
        {
            this.PreValidateSave();
            InferenceClassificationOverride inferenceClassificationOverride = this.Service.CreateInferenceClassificationOverride(this);
            this.propertyBag = inferenceClassificationOverride.propertyBag;
        }

        public async System.Threading.Tasks.Task DeleteAsync()
        {
            this.PreValidateDelete();
            await this.Service.DeleteInferenceClassificationOverrideAsync(this);
            this.propertyBag.Clear();
        }

        /// <summary>
        /// Delete override. 
        /// </summary>
        public void Delete()
        {
            this.PreValidateDelete();
            this.Service.DeleteInferenceClassificationOverride(this);
            this.propertyBag.Clear();
        }
    }
}