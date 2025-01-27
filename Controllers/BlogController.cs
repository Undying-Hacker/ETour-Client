﻿using Client.Models;
using Core.Entities;
using Core.Helpers;
using Core.Interfaces;
using Core.Services;
using Core.Value_Objects;
using Infrastructure.InterfaceImpls;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Client.Controllers
{
    public class BlogController : BaseController
    {
        private static readonly int _pageSize = 10;
        private readonly BlogFilterService _filterService;
        private readonly BlogRecommendationService _recommendationService;
        private readonly IPostRepository<Post, Employee> _postRepository;
        private readonly IUnitOfWork _unitOfWork;

        public BlogController(IPostRepository<Post, Employee> postRepository, BlogFilterService filterService, BlogRecommendationService recommendationService, IUnitOfWork unitOfWork)
        {
            _postRepository = postRepository;
            _filterService = filterService;
            _recommendationService = recommendationService;
            _unitOfWork = unitOfWork;
        }

        // Display list of blog posts
        // Parameter pageNumber specify which set of tours to display according to pageSize
        // Returns View(postList)
        public IActionResult Index(BlogFilterParams filterParams, int pageNumber = 1)
        {
            IEnumerable<IPost<Employee>> posts = _postRepository.Queryable
                .Include(p => p.Owner)
                .Include(p => p.Comments)
               .Select(p => (IPost<Employee>)p);

            var filteredPosts = _filterService.ApplyFilter(posts, filterParams);

            return View(new BlogListModel
            {
                Posts = PaginatedList<IPost<Employee>>.Create(filteredPosts.AsQueryable(), pageNumber, _pageSize),
                FilterParams = filterParams
            });
        }

        // Display detail of a blog post
        // Parameter id represent the id of the post, it is null when client does not provide id or given id cannot be converted to int
        // Return Notfound() when invalid id or no post with id is found, View(post) otherwise
        public async Task<IActionResult> Detail(int id)
        {
            var post = await _postRepository.Queryable
                .Include(p => p.Owner)
                .Include(p => p.Comments)
                .ThenInclude(cmt => cmt.Owner)
                .FirstOrDefaultAsync(p => p.ID == id);

            if (post == null)
            {
                return NotFound();
            }

            IEnumerable<IPost<Employee>> recommendCandidates = _postRepository.Queryable
                .Include(p => p.Owner)
                .Include(p => p.Comments)
               .Select(p => (IPost<Employee>)p);

            return View(new PostDetailModel
            {
                Post = post,
                Recommendations = _recommendationService.GetRecommendations(recommendCandidates, post)
            });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddComment(Comment comment)
        {
            var post = await _postRepository.FindAsync(comment.PostID);

            if (post == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                comment.OwnerID = UserID;
                comment.LastUpdated = DateTime.Now;
                await _postRepository.AddComment(comment);
                await _unitOfWork.CommitAsync();
            }

            return RedirectToAction(nameof(Detail), new { id = post.ID });
        }
    }
}
