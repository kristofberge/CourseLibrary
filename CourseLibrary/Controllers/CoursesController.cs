using AutoMapper;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Models;
using CourseLibrary.API.Repos;
using Marvin.Cache.Headers;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace CourseLibrary.API.Controllers
{
    [ApiController]
    [Route("api/authors/{authorId}/courses")]
    [HttpCacheExpiration(CacheLocation = CacheLocation.Public, MaxAge = 600)]
    [HttpCacheValidation(MustRevalidate = true)]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseLibraryRepository _repository;
        private readonly IMapper _mapper;

        public CoursesController(ICourseLibraryRepository courseLibraryRepository, IMapper mapper)
        {
            _repository = courseLibraryRepository ??
                throw new ArgumentNullException(nameof(courseLibraryRepository));
            _mapper = mapper ??
                throw new ArgumentNullException(nameof(mapper));
        }

        [HttpGet(Name = "GetCoursesForAuthor")]
        public ActionResult<IEnumerable<CourseDto>> GetCoursesForAuthor(Guid authorId)
        {
            if (!_repository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var coursesFromRepo = _repository.GetCourses(authorId);

            return Ok(_mapper.Map<IEnumerable<CourseDto>>(coursesFromRepo));
        }

        [HttpGet("{courseId}", Name = "GetCourseForAuthor")]
        [HttpCacheExpiration(CacheLocation = CacheLocation.Public, MaxAge = 1000)]
        [HttpCacheValidation(MustRevalidate = false)]
        public ActionResult<CourseDto> GetCourseForAuthor(Guid authorId, Guid courseId)
        {
            if (!_repository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var courseFromRepo = _repository.GetCourse(authorId, courseId);

            if (courseFromRepo == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<CourseDto>(courseFromRepo));
        }

        [HttpPost(Name = "CreateCourseForAuthor")]
        public ActionResult<CourseDto> CreateCourseForAuthor(Guid authorId, CourseForCreationDto courseCreationDto)
        {
            if (!_repository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var courseEntity = _mapper.Map<Course>(courseCreationDto);

            _repository.AddCourse(authorId, courseEntity);
            _repository.Save();

            var courseToReturn = _mapper.Map<CourseDto>(courseEntity);

            return CreatedAtAction(nameof(GetCourseForAuthor), new { authorId, courseId = courseEntity.Id }, courseToReturn);
        }

        [HttpPut("{courseId}")]
        public IActionResult UpdateCourse(Guid authorId, Guid courseId, CourseForUpdateDto courseForUpdating)
        {
            if (!_repository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var courseFromRepo = _repository.GetCourse(authorId, courseId);

            if (courseFromRepo == null)
            {
                return UpsertFromPut(authorId, courseId, courseForUpdating);
            }

            // The following statement will apply all the fields of courseForUpdating to courseFromRepo
            _ = _mapper.Map(courseForUpdating, courseFromRepo);

            _repository.UpdateCourse(courseFromRepo);
            _repository.Save();

            return NoContent();
        }

        [HttpPatch("{courseId}")]
        public IActionResult PartiallyUpdateCourseForAuthor(Guid authorId, Guid courseId, JsonPatchDocument<CourseForUpdateDto> patchDocument) // To be able to parse a patch document, we need to add nuget Microsoft.ApasNetCore.Mvc.NewtonsoftJson,
        {
            if (!_repository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var courseFromRepo = _repository.GetCourse(authorId, courseId);

            if (courseFromRepo == null)
            {
                return UpsertFromPatch(authorId, courseId, patchDocument);
            }

            var courseToPatch = _mapper.Map<CourseForUpdateDto>(courseFromRepo);
            patchDocument.ApplyTo(courseToPatch, ModelState);

            if (!TryValidateModel(courseToPatch))
            {
                return ValidationProblem(ModelState);
            }

            _mapper.Map(courseToPatch, courseFromRepo);

            _repository.UpdateCourse(courseFromRepo);
            _repository.Save();

            return NoContent();
        }

        [HttpDelete("{courseId}")]
        public IActionResult DeleteCourseForAuthor(Guid authorId, Guid courseId)
        {
            if (!_repository.AuthorExists(authorId))
            {
                return NotFound();
            }

            var courseFromRepo = _repository.GetCourse(authorId, courseId);

            if (courseFromRepo == null)
            {
                return NotFound();
            }

            _repository.DeleteCourse(courseFromRepo);
            _repository.Save();

            return NoContent();
        }

        public override ActionResult ValidationProblem([ActionResultObjectValue] ModelStateDictionary modelStateDictionary)
        {
            var options = HttpContext.RequestServices.GetRequiredService<IOptions<ApiBehaviorOptions>>();
            return (ActionResult)options.Value.InvalidModelStateResponseFactory(ControllerContext);
        }

        private IActionResult UpsertFromPut(Guid authorId, Guid courseId, CourseForUpdateDto courseForUpdating)
        {
            var courseToAdd = _mapper.Map<Course>(courseForUpdating);
            courseToAdd.Id = courseId;
            _repository.AddCourse(authorId, courseToAdd);
            _repository.Save();

            var courseToReturn = _mapper.Map<CourseDto>(courseToAdd);
            return CreatedAtRoute(nameof(GetCourseForAuthor), new { authorId, courseId = courseToReturn.Id }, courseToReturn);
        }

        private IActionResult UpsertFromPatch(Guid authorId, Guid courseId, JsonPatchDocument<CourseForUpdateDto> patchDocument)
        {
            var courseDto = new CourseForUpdateDto();
            patchDocument.ApplyTo(courseDto, ModelState);

            if (!TryValidateModel(courseDto))
            {
                return ValidationProblem(ModelState);
            }

            var courseToAdd = _mapper.Map<Course>(courseDto);
            courseToAdd.Id = courseId;

            _repository.AddCourse(authorId, courseToAdd);
            _repository.Save();

            var courseToReturn = _mapper.Map<CourseDto>(courseToAdd);
            return CreatedAtRoute(nameof(GetCourseForAuthor), new { authorId, courseId = courseToReturn.Id }, courseToReturn);
        }
    }
}