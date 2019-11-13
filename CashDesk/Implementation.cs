using CashDesk.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CashDesk
{
    /// <inheritdoc />
    public class DataAccess : IDataAccess
    {
        private CashDesktDataContext context;
        /// <inheritdoc />
        public Task InitializeDatabaseAsync()
        {
            if (context != null)
            {
                throw new InvalidOperationException();
            }

            context = new CashDesktDataContext();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<int> AddMemberAsync(string firstName, string lastName, DateTime birthday)
        {
            if (context == null)
            {
                throw new InvalidOperationException();
            }
            if (string.IsNullOrWhiteSpace(firstName)
                || string.IsNullOrWhiteSpace(lastName))
            {
                throw new ArgumentException();
            }
            if (await context.Member.AnyAsync(m => m.LastName == lastName))
            {
                throw new DuplicateNameException();
            }
            var newMember = new Member
            {
                FirstName = firstName,
                LastName = lastName,
                Birthday = birthday
            };
            context.Member.Add(newMember);

            await context.SaveChangesAsync();

            return newMember.MemberNumber;

        }

        /// <inheritdoc />
        public async Task DeleteMemberAsync(int memberNumber)
        {
            if (context == null)
            {
                throw new InvalidOperationException();
            }

            if (!await context.Member.AnyAsync(m => m.MemberNumber == memberNumber))
            {
                throw new ArgumentException();
            }

            Member member = await context.Member.FirstAsync(m => m.MemberNumber == memberNumber);
            context.Member.Remove(member);
            await context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<IMembership> JoinMemberAsync(int memberNumber)
        {
            if (context == null)
            {
                throw new InvalidOperationException();
            }

            if (!await context.Member.AnyAsync(m => m.MemberNumber == memberNumber))
            {
                throw new ArgumentException();
            }

            Member member = await context.Member.FirstAsync(m => m.MemberNumber == memberNumber);

            if (member.Memberships == null)
            {
                member.Memberships = new List<Membership>();
            }

            if (member.Memberships.Count == 0
                || member.Memberships[member.Memberships.Count - 1].End != DateTime.MaxValue)
            {
                Membership membership = new Membership
                {
                    Member = member,
                    Begin = DateTime.Now
                };
                context.Membership.Add(membership);
                context.SaveChanges();

                return membership;
            }
            else
            {
                throw new AlreadyMemberException();
            }

        }


        /// <inheritdoc />
        public async Task<IMembership> CancelMembershipAsync(int memberNumber)
        {
            Membership membership;

            if (context == null)
            {
                throw new InvalidOperationException();
            }

            if (!await context.Membership.AnyAsync(m => m.Member.MemberNumber == memberNumber
            && m.End == DateTime.MaxValue))
            {
                throw new NoMemberException();

            }
            membership = await context.Membership.FirstAsync(m => m.Member.MemberNumber == memberNumber
            && m.End == DateTime.MaxValue);
            membership.End = DateTime.Now;

            await context.SaveChangesAsync();

            return membership;
        }

        /// <inheritdoc />
        public async Task DepositAsync(int memberNumber, decimal amount)
        {
            if (context == null)
            {
                throw new InvalidOperationException();
            }
            if (!await context.Member.AnyAsync(m => m.MemberNumber == memberNumber))
            {
                throw new ArgumentException();
            }
            if (amount < 0)
            {
                throw new ArgumentException();
            }

            Member member;
            member = await context.Member.FirstAsync(m => m.MemberNumber == memberNumber);

            if (member.Memberships != null
                && member.Memberships.Count > 0
                && member.Memberships[member.Memberships.Count - 1].End > DateTime.Now)
            {
                Membership membership = member.Memberships[member.Memberships.Count - 1];

                Deposit deposit = new Deposit
                {
                    Membership = membership,
                    Amount = amount
                };

                context.Deposit.Add(deposit);
                context.SaveChanges();
            }
            else
            {
                throw new NoMemberException();
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<IDepositStatistics>> GetDepositStatisticsAsync()
        {
            if (context == null)
            {
                throw new InvalidOperationException();
            }
            List<Member> members = await context.Member.ToListAsync();
            List<DepositStatistics> statistics = new List<DepositStatistics>();
            foreach (var curMember in members)
            {
                DepositStatistics curSatistic = new DepositStatistics
                {
                    Member = curMember
                };
                if (curMember.Memberships != null && curMember.Memberships.Count > 0
                    && curMember.Memberships[curMember.Memberships.Count - 1].End == DateTime.MaxValue)
                {
                    Membership membership = curMember.Memberships[curMember.Memberships.Count - 1];
                    decimal curTotalAmount = 0;
                    foreach (var curDeposit in membership.Deposits)
                    {
                        curTotalAmount += curDeposit.Amount;
                    }
                    curSatistic.TotalAmount = curTotalAmount;
                }
                else
                {
                    curSatistic.TotalAmount = 0;
                }

                statistics.Add(curSatistic);
            }

            return statistics;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (context != null)
            {
                context.Dispose();
                context = null;
            }
        }
    }
}
