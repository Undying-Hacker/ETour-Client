﻿using Core.Entities;
using Infrastructure.InterfaceImpls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Client.Models
{
    public class PostDetailModel
    {
        public Post Post { get; set; }
        public Comment Comment { get; set; }
        public IEnumerable<IPost<Employee>> Recommendations { get; set; }
    }
}
