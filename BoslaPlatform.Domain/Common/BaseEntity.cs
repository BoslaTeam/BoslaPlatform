using System.ComponentModel.DataAnnotations.Schema;

namespace BoslaPlatform.Domain.Common
{
    public abstract class BaseEntity
    {
        public Guid Id { get; protected set; }
        private readonly List<DomainEvent> _domainEvents = [];
        [NotMapped]
        public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected BaseEntity() { }
        protected BaseEntity(Guid id)
        {
            Id = id == Guid.Empty ? Guid.NewGuid() : id;
        }
        public void AddDomainEvent(DomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }
        public void RemoveDomainEvent(DomainEvent domainEvent)
        {
            _domainEvents.Remove(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }

}
