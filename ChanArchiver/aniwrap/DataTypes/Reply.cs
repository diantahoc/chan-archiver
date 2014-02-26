using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AniWrap.DataTypes
{
    public class Reply : GenericPost
    {
        public Thread Owner { get; private set; }

        public Reply(Thread owner, GenericPost i)
        {
            this.Owner = owner;
            base.Board = owner.Board;

            base.Comment = i.Comment;
            base.Email = i.Email;
            base.File = i.File;
            base.Name = i.Name;
            base.ID = i.ID;
            base.Subject = i.Subject;
            base.Time = i.Time;
            base.Trip = i.Trip;
            base.country_name = i.country_name;
            base.country_flag = i.country_flag;
            base.Capcode = i.Capcode;
        }
    }
}
