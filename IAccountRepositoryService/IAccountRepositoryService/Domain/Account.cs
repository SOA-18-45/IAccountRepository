using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Contracts;

namespace IAccountRepositoryService.Domain
{
    public class Account
    {
        public virtual Guid Id { get; set; }
        public virtual Guid ClientId { get; set; }
        public virtual string AccountNumber { get; set; }
        public virtual double Money { get; set; }
        public virtual string Type { get; set; }
        public virtual double Percentage { get; set; }
        public virtual DateTime EndDate { get; set; }
        public virtual DateTime StartDate { get; set; }

        public Account()
        { }

        public Account(AccountDetails details)
        {
            this.Id = details.Id;
            this.ClientId = details.ClientId;
            this.AccountNumber = details.AccountNumber;
            this.Money = details.Money;
            this.Type = details.Type;
            this.Percentage = details.Percentage;
            this.EndDate = details.EndDate;
            this.StartDate = details.StartDate;
        }
    }
}
