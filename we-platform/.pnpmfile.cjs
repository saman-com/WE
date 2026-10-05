function readPackage(pkg) {
  if (pkg.name === "next" && pkg.dependencies?.postcss) {
    pkg.dependencies.postcss = "8.5.28";
  }
  return pkg;
}

module.exports = {
  hooks: {
    readPackage,
  },
};
