using System;
using Microsoft.EntityFrameworkCore;


namespace PodCastBot{
    public class DB: DbContext
    {
        //строка
        protected override void OnConfiguring( DbContextOptionsBuilder optBilder){
            optBilder.UseNpgsql(@"Server=104.154.209.187; User Id=postgres;Password=afterSolvingThatProblemIwillDeleteThis; Database=postgres;Trusted_Connection=True;");
        }
        public DbSet<Podcast> TPodcasts;
        //public DbSet<

        public class Podcast{
            public int ID;
            public string Text;
            public int WhoAddedIt;

        }

    }



}