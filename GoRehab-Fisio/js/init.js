function init() {
  $(".rutinas").on("click", function( event ){
  	event.preventDefault();
  	location.href = "rutinas.html";
  	//alert("terapeuta");
  });
  $(".terapeuta").on("click", function( event ){
  	event.preventDefault();
  	location.href = "chat.html";
  	//alert("terapeuta");
  });

  $(".inner-circle").on("click", function( event ){
  	event.preventDefault();
  	location.href = "gorehab.html";
  	//alert("terapeuta");
  });
}

var initFunction = init();
initFunction(); 