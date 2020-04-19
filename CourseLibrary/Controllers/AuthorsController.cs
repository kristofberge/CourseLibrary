using AutoMapper;
using CourseLibrary.API.ActionConstraints;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.Repos;
using CourseLibrary.API.ResourceParameters;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace CourseLibrary.API.Controllers
{
    [ApiController]
    [Route("api/authors")]
    public class AuthorsController : ControllerBase
    {
        private readonly ICourseLibraryRepository _repository;
        private readonly IMapper _mapper;
        private readonly IPropertyMappingService _propertyMappingService;
        private readonly IPropertyCheckerService _propertyCheckerService;

        public AuthorsController(
            ICourseLibraryRepository repository,
            IMapper mapper,
            IPropertyMappingService propertyMappingService,
            IPropertyCheckerService propertyCheckerService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _propertyMappingService = propertyMappingService ?? throw new ArgumentNullException(nameof(propertyMappingService));
            _propertyCheckerService = propertyCheckerService;
        }

        [HttpGet(Name = "GetAuthors")]
        [HttpHead]
        public IActionResult GetAuthors([FromQuery] AuthorsResourceParameters parameters)
        {
            if (!_propertyMappingService.ValidMappingExistsFor<AuthorDto, Author>(parameters.OrderBy))
            {
                return BadRequest();
            }

            if (!_propertyCheckerService.TypeHasProperties<AuthorDto>(parameters.Fields))
            {
                return BadRequest();
            }

            var authorsFromRepo = _repository.GetAuthors(parameters);

            var shapedAuthors = _mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo).ShapeData(parameters.Fields);
            var shapedAuthorsWithLinks = shapedAuthors.Select(author =>
            {
                var dictionary = author as IDictionary<string, object>;
                var authorLinks = CreateLinksForAuthor((Guid)dictionary["Id"]);
                dictionary.Add("links", authorLinks);
                return dictionary;
            });
            var links = CreateLinksForAuthors(parameters, authorsFromRepo.HasNext, authorsFromRepo.HasPrevious);
            var linkedCollectionResource = new { value = shapedAuthorsWithLinks, links };

            AddPaginationMetaDataToHeader(authorsFromRepo);

            return Ok(linkedCollectionResource);
        }

        [Produces(
            "application/json",
            "application/vnd.marvin.hateoas+json",
            "application/vnd.marvin.author.full+json",
            "application/vnd.marvin.author.full.hateoas+json",
            "application/vnd.marvin.author.friendly+json",
            "application/vnd.marvin.author.friendly.hateoas+json")]
        [HttpGet("{authorId}", Name = "GetAuthor")]
        public IActionResult GetAuthor(Guid authorId, string fields, [FromHeader] string accept)
        {
            if (!MediaTypeHeaderValue.TryParse(accept, out MediaTypeHeaderValue mediaType))
            {
                return BadRequest();
            }

            Author authorFromRepo = _repository.GetAuthor(authorId);

            if (authorFromRepo == null)
            {
                return NotFound();
            }

            if (!_propertyCheckerService.TypeHasProperties<AuthorDto>(fields))
            {
                return BadRequest();
            }

            bool includeLinks = mediaType.SubTypeWithoutSuffix.EndsWith("hateoas", StringComparison.InvariantCultureIgnoreCase);

            var primaryMediaType = includeLinks ?
                mediaType.SubTypeWithoutSuffix.Substring(0, mediaType.SubTypeWithoutSuffix.Length - 8) :
                mediaType.SubTypeWithoutSuffix;


            var linkedResourceToReturn = primaryMediaType.Equals("vnd.marvin.author.full") ?
                _mapper.Map<AuthorFullDto>(authorFromRepo).ShapeData(fields) as IDictionary<string, object> :
                _mapper.Map<AuthorDto>(authorFromRepo).ShapeData(fields) as IDictionary<string, object>;

            if (includeLinks)
            {
                linkedResourceToReturn.Add("links", CreateLinksForAuthor(authorId, fields));
            }

            return Ok(linkedResourceToReturn);
        }

        [HttpPost(Name = "CreateAuthorWithDateOfDeath")]
        [RequestHeaderMatchesMediaType("Content-Type",
            "application/vnd.marvin.authorforcreationwithdateofdeath+json")]
        [Consumes("application/vnd.marvin.authorforcreationwithdateofdeath+json")]
        public IActionResult CreateAuthorWithDateOfDeath(AuthorForCreationWithDateOfDeathDto authorToAdd)
        {
            Author authorEntity = _mapper.Map<Author>(authorToAdd);

            _repository.AddAuthor(authorEntity);
            _repository.Save();

            var linkResourceToReturn = _mapper.Map<AuthorDto>(authorEntity).ShapeData() as IDictionary<string, object>;
            var links = CreateLinksForAuthor(authorEntity.Id);
            linkResourceToReturn.Add("links", links);

            return CreatedAtRoute("GetAuthor", new { authorId = authorEntity.Id }, linkResourceToReturn);
        }

        [HttpPost(Name = "CreateAuthor")]
        [RequestHeaderMatchesMediaType("Content-Type",
            "application/json",
            "application/vnd.marvin.authorforcreation+json")]
        [Consumes("application/json", "application/vnd.marvin.authorforcreation+json")]
        public ActionResult<AuthorDto> CreateAuthor(AuthorForCreationDto authorToAdd)
        {
            Author authorEntity = _mapper.Map<Author>(authorToAdd);

            _repository.AddAuthor(authorEntity);
            _repository.Save();

            var linkResourceToReturn = _mapper.Map<AuthorDto>(authorEntity).ShapeData() as IDictionary<string, object>;
            var links = CreateLinksForAuthor(authorEntity.Id);
            linkResourceToReturn.Add("links", links);

            return CreatedAtRoute("GetAuthor", new { authorId = authorEntity.Id }, linkResourceToReturn);
        }

        [HttpOptions]
        public IActionResult GetAuthorOptions()
        {
            Response.Headers.Add("Allow", "GET,OPTIONS,POST");
            return Ok();
        }

        [HttpDelete("{authorId}", Name = "DeleteAuthor")]
        public IActionResult DeleteAuthor(Guid authorId)
        {
            var authorToDelete = _repository.GetAuthor(authorId);

            if (authorToDelete == null)
            {
                return NotFound();
            }

            _repository.DeleteAuthor(authorToDelete);
            _repository.Save();

            return NoContent();
        }

        private void AddPaginationMetaDataToHeader(PagedList<Author> authorsFromRepo)
        {
            var paginationMetaData = new
            {
                totalCount = authorsFromRepo.TotalCount,
                pageSize = authorsFromRepo.PageSize,
                currentPage = authorsFromRepo.CurrentPage,
                totalPages = authorsFromRepo.TotalPages
            };

            Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationMetaData));
        }

        private string CreateAuthorsResourceUri(AuthorsResourceParameters parameters, ResurceUriType type)
        {
            switch (type)
            {
                case ResurceUriType.PreviousPage:
                    return Url.Link("GetAuthors", new
                    {
                        fields = parameters.Fields,
                        orderBy = parameters.OrderBy,
                        pageNumber = parameters.PageNumber - 1,
                        pageSize = parameters.PageSize,
                        mainCategory = parameters.MainCategory,
                        searchQuery = parameters.SearchQuery
                    });
                case ResurceUriType.NextPage:
                    return Url.Link("GetAuthors", new
                    {
                        fields = parameters.Fields,
                        orderBy = parameters.OrderBy,
                        pageNumber = parameters.PageNumber + 1,
                        pageSize = parameters.PageSize,
                        mainCategory = parameters.MainCategory,
                        searchQuery = parameters.SearchQuery
                    });
                case ResurceUriType.Current:
                default:
                    return Url.Link("GetAuthors", new
                    {
                        fields = parameters.Fields,
                        orderBy = parameters.OrderBy,
                        pageNumber = parameters.PageNumber,
                        pageSize = parameters.PageSize,
                        mainCategory = parameters.MainCategory,
                        searchQuery = parameters.SearchQuery
                    });
            }
        }

        private IEnumerable<LinkDto> CreateLinksForAuthor(Guid authorId, string fields = null)
        {
            return new List<LinkDto>()
            {
                string.IsNullOrWhiteSpace(fields) ?
                    new LinkDto(Url.Link("GetAuthor", new { authorId }), "self", "GET") :
                    new LinkDto(Url.Link("GetAuthor", new { authorId, fields }), "self", "GET"),
                new LinkDto(Url.Link("DeleteAuthor", new { authorId }), "delete_author", "DELETE"),
                new LinkDto(Url.Link("CreateCourseForAuthor", new { authorId }), "create_course_for_author", "POST"),
                new LinkDto(Url.Link("GetCoursesForAuthor", new { authorId }), "courses", "GET")
            };
        }

        private IEnumerable<LinkDto> CreateLinksForAuthors(AuthorsResourceParameters parameters, bool hasNext, bool hasPrevious)
        {
            var links = new List<LinkDto>
            {
                new LinkDto(CreateAuthorsResourceUri(parameters, ResurceUriType.Current), "self", "GET"),
            };

            if (hasNext)
            {
                links.Add(new LinkDto(CreateAuthorsResourceUri(parameters, ResurceUriType.NextPage), "next-page", "GET"));
            }

            if (hasPrevious)
            {
                links.Add(new LinkDto(CreateAuthorsResourceUri(parameters, ResurceUriType.PreviousPage), "previous-page", "GET"));
            }

            return links;
        }
    }
}
