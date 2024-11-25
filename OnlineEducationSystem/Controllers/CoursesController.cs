﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using OnlineEducationSystem.Helpers;
using OnlineEducationSystem.Models;

namespace OnlineEducationSystem.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CoursesController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly DatabaseHelper _dbHelper;

    public CoursesController(IConfiguration configuration)
    {
        _configuration = configuration;
        var connectionString = configuration.GetConnectionString("PostgreSqlConnection");
        _dbHelper = new DatabaseHelper(connectionString!);
    }

    [HttpGet]
    public IActionResult GetCourses()
    {
        var query = "SELECT * FROM courses";
        var courses = _dbHelper.ExecuteReader(query, reader => new Courses
        {
            course_id = reader.GetInt32(0),
            instructor_id = reader.GetInt32(1),
            title = reader.GetString(2),
            description = reader.IsDBNull(3) ? null : reader.GetString(3),
            created_at = reader.GetDateTime(4),
            updated_at = reader.GetDateTime(5),
            deleted_at = reader.IsDBNull(6) ? null : reader.GetDateTime(6)
        });
        // remove deleted courses
        courses = courses.Where(course => course.deleted_at == null).ToList();
        
        return Ok(courses);
    }
    [HttpGet("{id}")]
    public IActionResult GetCourse(int id)
    {
        var query = "SELECT * FROM courses WHERE course_id = @id";
        var parameters = new NpgsqlParameter[]
        {
            new NpgsqlParameter("@id", id)
        };

        var course = _dbHelper.ExecuteReader(query, reader => new Courses
        {
            course_id = reader.GetInt32(0),
            instructor_id = reader.GetInt32(1),
            title = reader.GetString(2),
            description = reader.IsDBNull(3) ? null : reader.GetString(3),
            created_at = reader.GetDateTime(4),
            updated_at = reader.GetDateTime(5),
            deleted_at = reader.IsDBNull(6) ? null : reader.GetDateTime(6)
        }, parameters).FirstOrDefault();

        if (course == null || course.deleted_at != null)
        {
            return NotFound();
        }

        return Ok(course);
    }

    [Authorize(Roles = "instructor")]
    [HttpPost]
    public IActionResult CreateCourse([FromBody] CreateCourses course)
    {
        var query = "INSERT INTO courses (instructor_id, title, description) VALUES (@instructor_id, @title, @description)";
        var parameters = new NpgsqlParameter[]
        {
            new NpgsqlParameter("@instructor_id", course.instructor_id),
            new NpgsqlParameter("@title", course.title),
            new NpgsqlParameter("@description", course.description)
        };

        var courseId = _dbHelper.ExecuteNonQuery(query, parameters);
        return Ok(courseId);
    }

    [Authorize(Roles = "instructor,admin")]
    [HttpPatch]
    public IActionResult UpdateCourse([FromBody] PatchCourses course)
    {
        var query = "UPDATE courses SET title = @title, description = @description WHERE course_id = @course_id";
        var parameters = new NpgsqlParameter[]
        {
            new NpgsqlParameter("@course_id", course.course_id),
            new NpgsqlParameter("@title", course.title),
            new NpgsqlParameter("@description", course.description)
        };

        _dbHelper.ExecuteNonQuery(query, parameters);

        return Ok();
    }

    [Authorize(Roles = "instructor,admin")]
    [HttpDelete("{id}")]
    public IActionResult DeleteCourse(int id)
    {
        var query = "UPDATE courses SET deleted_at = @deleted_at WHERE course_id = @course_id";
        var parameters = new NpgsqlParameter[]
        {
            new NpgsqlParameter("@course_id", id),
            new NpgsqlParameter("@deleted_at", DateTime.Now)
        };

        _dbHelper.ExecuteNonQuery(query, parameters);

        return Ok();
    }
}