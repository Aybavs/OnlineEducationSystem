﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using OnlineEducationSystem.Helpers;
using OnlineEducationSystem.Models;

namespace OnlineEducationSystem.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ExamsController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly DatabaseHelper _dbHelper;

    public ExamsController(IConfiguration configuration)
    {
        _configuration = configuration;
        var connectionString = configuration.GetConnectionString("PostgreSqlConnection");
        _dbHelper = new DatabaseHelper(connectionString!);
    }

    [HttpGet]
    public IActionResult GetExams()
    {
        var query = "SELECT * FROM exams";
        var exams = _dbHelper.ExecuteReader(query, reader => new Exams
        {
            exam_id = reader.GetInt32(0),
            course_id = reader.GetInt32(1),
            title = reader.GetString(2),
            description = reader.IsDBNull(3) ? null : reader.GetString(3),
            created_at = reader.GetDateTime(4),
            updated_at = reader.GetDateTime(5),
            deleted_at = reader.IsDBNull(6) ? null : reader.GetDateTime(6)
        }).Where(exam => exam.deleted_at == null).ToList();

        return Ok(exams);
    }

    [HttpGet("{id}")]
    public IActionResult GetExam(int id)
    {
        var query = "SELECT * FROM exams WHERE exam_id = @id";
        var parameters = new NpgsqlParameter[]
        {
            new NpgsqlParameter("@id", id)
        };

        var exam = _dbHelper.ExecuteReader(query, reader => new Exams
        {
            exam_id = reader.GetInt32(0),
            course_id = reader.GetInt32(1),
            title = reader.GetString(2),
            description = reader.IsDBNull(3) ? null : reader.GetString(3),
            created_at = reader.GetDateTime(4),
            updated_at = reader.GetDateTime(5),
            deleted_at = reader.IsDBNull(6) ? null : reader.GetDateTime(6)
        }, parameters).FirstOrDefault();

        if (exam == null || exam.deleted_at != null)
        {
            return NotFound();
        }

        return Ok(exam);
    }

    [Authorize(Roles = "instructor")]
    [HttpPost]
    public IActionResult CreateExam([FromBody] CreateExams exam)
    {
        var query = "INSERT INTO exams (course_id, title, description) VALUES (@course_id, @title, @description)";
        var parameters = new NpgsqlParameter[]
        {
            new NpgsqlParameter("@course_id", exam.course_id),
            new NpgsqlParameter("@title", exam.title),
            new NpgsqlParameter("@description", exam.description)
        };

        var examId = _dbHelper.ExecuteNonQuery(query, parameters);
        return Ok(examId);
    }

    [Authorize(Roles = "instructor")]
    [HttpPatch]
    public IActionResult UpdateExam([FromBody] PatchExams exam)
    {
        var query = "UPDATE exams SET title = @title, description = @description WHERE exam_id = @exam_id";
        var parameters = new NpgsqlParameter[]
        {
            new NpgsqlParameter("@exam_id", exam.exam_id),
            new NpgsqlParameter("@title", exam.title),
            new NpgsqlParameter("@description", exam.description)
        };

        _dbHelper.ExecuteNonQuery(query, parameters);
        return Ok();
    }

    [Authorize(Roles = "instructor,admin")]
    [HttpDelete("{id}")]
    public IActionResult DeleteExam(int id)
    {
        var query = "UPDATE exams SET deleted_at = @deleted_at WHERE exam_id = @exam_id";
        var parameters = new NpgsqlParameter[]
        {
            new NpgsqlParameter("@exam_id", id),
            new NpgsqlParameter("@deleted_at", DateTime.Now)
        };

        _dbHelper.ExecuteNonQuery(query, parameters);
        return Ok();
    }
}