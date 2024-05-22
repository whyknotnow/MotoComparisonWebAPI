
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;

namespace MotoComparisonWebAPI.Context
{

    public class MotorcycleContext : DbContext
    {
        public MotorcycleContext(DbContextOptions<MotorcycleContext> options) : base(options) { }

        public DbSet<Manufacturer> Manufacturers { get; set; }
        public DbSet<Model> Models { get; set; }
        public DbSet<Specification> Specifications { get; set; }
    }

    public class Manufacturer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public List<Model> Models { get; set; }
    }

    public class Model
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public int ManufacturerId { get; set; }
        public Manufacturer Manufacturer { get; set; }
        public List<Specification> Specifications { get; set; }
    }

    public class Specification
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public int ModelId { get; set; }
        public Model Model { get; set; }
    }

}
