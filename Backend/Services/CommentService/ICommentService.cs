﻿using ProjectManagementSystem1.Model.Entities;

namespace ProjectManagementSystem1.Services.CommentService.CommentService
{
    public interface ICommentService
    {
        Task AddCommentAsync(int taskId, string memberId, string content);
        Task<List<Comment>> GetCommentsByTaskIdAsync(int taskId);
    }
}
