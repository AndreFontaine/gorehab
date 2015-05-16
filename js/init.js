function init() {
  var nombre = "Mozilla";
  function muestraNombre() {
    console.log(nombre);
  }
  return muestraNombre;
}

var initFunction = init();
initFunction(); 